using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
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
        public static async Task<bool> IsExistUser(string id)
        {
            var ans = false;
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = $"SELECT * FROM Users WHERE Id = '{id}'";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                    ans = true;
                conn.Close();
            }
            return ans;
        }
        public static async Task<List<StatItem>> GetUserStatById(string id)
        {
            var ans = new List<StatItem> { };
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
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
                        var subTemp = item.Split();
                        ans.Add(new StatItem(subTemp[0], subTemp[1]));
                    }
                }
                conn.Close();
            }
            return ans;
        }
        public static async Task FillStatWithList(List<StatItem> list, string id)
        {
            list = list.OrderByDescending(item => item.ErrorCount).ToList();
            var ans = "";
            foreach (var item in list)
            {
                ans += item.ToString() + "&";
            }
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
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
        public static async Task UpdateUserStat(string id, string verb)
        {
            var stat = await GetUserStatById(id);
            if (stat.FirstOrDefault(item => item.Verb == verb) == null)
            {
                stat.Add(new StatItem(verb, "1"));
            }
            else
            {
                stat.First(item => item.Verb == verb).Increment();
            }
            await FillStatWithList(stat, id);
        }

        static public async Task InitialiseUser(Message msg)
        {
            if (!(await DbUser.IsExistUser(msg.Chat.Id.ToString())))
            {
                using (SqliteConnection conn = new("data source = ..\\..\\..\\VerbsDb.db"))
                {
                    using (SqliteCommand cmd = new SqliteCommand())
                    {
                        string strSql = $"INSERT INTO[Users] ([Id], [Name], [Stat]) VALUES('{msg.Chat.Id}','{msg.From.FirstName}', '')";
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
