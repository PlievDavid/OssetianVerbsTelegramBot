using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
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
        public static readonly string dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");
        private static Dictionary<string, List<StatItem>> tempStat = new Dictionary<string, List<StatItem>>();
        private static Dictionary<string, int> tempScore = new Dictionary<string, int>();
        private static Dictionary<string, int> tempStreak = new Dictionary<string, int>();
        public static readonly List<RatingItem> tempRating = new List<RatingItem>();
        public static readonly HashSet<long> allUsersId = new HashSet<long>();
        public static bool IsExistUser(long id) => allUsersId.Contains(id);

        public static async Task InitializeAllUsers()
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                string sql = "SELECT Id FROM Users";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    allUsersId.Add(reader.GetInt64(0));
            }
        }

        public static async Task<List<StatItem>> GetUserStatById(string id)
        {
            var ans = new List<StatItem> { };
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = $"SELECT Stat, Streak FROM Users WHERE Id = '{id}'";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    tempStreak[id] = Convert.ToInt32(reader[reader.FieldCount - 1]);
                    var temp = reader[0].ToString().Split("&");
                    foreach (var item in temp)
                    {
                        if (string.IsNullOrEmpty(item))
                            return ans;
                        ans.Add(new StatItem(item));
                    }
                }
            }
            return ans;
        }
        public static async Task UpdateUserRating()
        {
            tempRating.Clear();
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                string sql = $"SELECT Id, Name, DailyScore, WeeklyScore, MonthlyScore FROM Users";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var id = Convert.ToInt64(reader.GetString(0));
                    tempRating.Add(new RatingItem(id , reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)));
                }
            }
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
                    string sql = $"Update Users Set Stat = '{ans}', DailyScore = DailyScore + {tempScore[id]}, WeeklyScore = WeeklyScore + {tempScore[id]}, MonthlyScore = MonthlyScore + {tempScore[id]} WHERE Id = '{id}'";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static async Task StartStatUpdate(string id)
        {
            tempStat[id] = await GetUserStatById(id);
            tempScore[id] = 0;
        }
        public static async Task UpdateUserStat(string id, string verb, bool IsError)
        {
            if (tempStat[id].FirstOrDefault(item => item.Verb == verb) == null)
            {
                if (IsError)
                    tempStat[id].Add(new StatItem(verb, "0", "1"));
                else
                {
                    tempStat[id].Add(new StatItem(verb, "1", "1"));
                    tempScore[id] += 10 + StreakMultiplier(id);
                }
            }
            else
            {
                if (IsError)
                    tempStat[id].First(item => item.Verb == verb).IncrementCount();
                else
                {
                    tempStat[id].First(item => item.Verb == verb).IncrementRightCount();
                    tempScore[id] += 10 + StreakMultiplier(id);
                }
            }
        }
        static private int StreakMultiplier(string id)
        {
            return tempStreak[id] >= 50 ? 100 : tempStreak[id] * 2;
        }
        static public async Task InitialiseUser(Message msg)
        {
            if (!IsExistUser(msg.Chat.Id))
            {
                allUsersId.Add(msg.Chat.Id);
                var date = DateTime.Now.ToShortDateString();
                using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    using (SqliteCommand cmd = new SqliteCommand())
                    {
                        string strSql = $"INSERT INTO[Users] ([Id], [Name], [Stat], [Date]) VALUES('{msg.Chat.Id}','{msg.From?.FirstName ?? "undefined"}', '', '{date}')";
                        cmd.CommandText = strSql;
                        cmd.Connection = conn;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        
    }
}
