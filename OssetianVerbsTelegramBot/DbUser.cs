using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot
{
    public static class DbUser
    {
        private static readonly string dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");
        private static Dictionary<string,List<StatItem>> tempStat = new Dictionary<string, List<StatItem>>();
        public static readonly HashSet<long> allUsersId = new HashSet<long>();
        public static bool IsExistUser(long id) => allUsersId.Contains(id);

        public static async Task InitializeAllUsers()
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT Id FROM Users";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    allUsersId.Add(reader.GetInt64(0));
                conn.Close();
            }
        }

        public static async Task<List<StatItem>> GetUserStatById(string id)
        {
            var ans = new List<StatItem> { };
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = $"SELECT * FROM Users WHERE Id = '{id}'";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var temp = reader[2].ToString().Split("&");
                    foreach (var item in temp)
                    {
                        if (string.IsNullOrEmpty(item))
                            return ans;
                        ans.Add(new StatItem(item));
                    }
                }
                conn.Close();
            }
            return ans;
        }

        public static async Task FillStat(string id)
        {
            var list = tempStat[id].OrderByDescending(item => item.Percent).ThenByDescending(item => item.Count).ThenByDescending(item => item.RightCount).ToList();
            tempStat.Remove(id);
            var ans = "";
            foreach (var item in list)
            {
                ans += item.ToString() + "&";
            }
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    conn.Open();
                    string sql = $"Update Users Set Stat = '{ans}' WHERE Id = '{id}'";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        public static async Task StartStatUpdate(string id)
        {
            tempStat[id] = await GetUserStatById(id);
        }
        public static async Task UpdateUserStat(string id, string verb, bool IsError)
        {
            if (tempStat[id].FirstOrDefault(item => item.Verb == verb) == null)
            {
                if (IsError)
                    tempStat[id].Add(new StatItem(verb, "0", "1"));
                else
                    tempStat[id].Add(new StatItem(verb, "1", "1"));
            }
            else
            {
                if (IsError)
                    tempStat[id].First(item => item.Verb == verb).IncrementCount();
                else
                    tempStat[id].First(item => item.Verb == verb).IncrementRightCount();
            }
        }

        static public async Task InitialiseUser(Message msg)
        {
            if (! IsExistUser(msg.Chat.Id))
            {
                allUsersId.Add(msg.Chat.Id);
                var date = DateTime.Now.ToShortDateString();
                using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    using (SqliteCommand cmd = new SqliteCommand())
                    {
                        string strSql = $"INSERT INTO[Users] ([Id], [Name], [Stat], [Date]) VALUES('{msg.Chat.Id}','{msg.From?.FirstName??"undefined"}', '', '{date}')";
                        cmd.CommandText = strSql;
                        cmd.Connection = conn;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
        }
    }
}
