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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace OssetianVerbsTelegramBot
{
    public static class DbUser
    {
        public static readonly string dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");

        private static Dictionary<string, List<StatItem>> tempStat = new Dictionary<string, List<StatItem>>();
        private static Dictionary<string, int> tempScore = new Dictionary<string, int>();

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
            var result = new List<StatItem> { };
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                string sql = $"SELECT Verb, Correct, Incorrect FROM UsersWordStatistic WHERE UserId = {id} " +
                    $"ORDER BY Correct DESC";
                await conn.OpenAsync();
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var verb = reader.GetString(0);
                            int correct = reader.GetInt32(1);
                            int incorrect = reader.GetInt32(2);

                            var statItem = new StatItem(verb, correct, incorrect);
                            result.Add(statItem);
                        }
                    }
                }
            }
            return result;
        }

        public static async Task UpdateUserRating()
        {
            tempRating.Clear();
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT s.Id, u.Name, s.Daily, s.Weekly, s.Monthly 
                    FROM Score s
                    JOIN Users u ON s.Id = u.Id";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    SqliteDataReader reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var id = Convert.ToInt64(reader.GetString(0));
                        tempRating.Add(new RatingItem(id, reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)));
                    }
                }
            }
        }

        /// <summary>
        /// Заносит данные с кэша в Базу Данных и очищает из кэше пользователя
        /// </summary>
        public static async Task FillStat(string id)
        {
            var stats = tempStat[id];
            tempStat.Remove(id);

            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();

                foreach (var stat in stats)
                {
                    string sql =
                        $@"INSERT INTO UsersWordStatistic (UserId, Verb, Correct, Incorrect, TotalCount)
                        VALUES ('{id}', '{stat.Verb}', {stat.CorrectCount}, {stat.IncorrectCount}, {stat.TotalCount})
                        ON CONFLICT(UserId, Verb) DO UPDATE SET
                            Correct = excluded.Correct,
                            Incorrect = excluded.Incorrect,
                            TotalCount = excluded.TotalCount";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }


        public static async Task FillScore(string id)
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();

                string sql =
                     $@"INSERT OR REPLACE INTO Score (Id, Daily, Weekly, Monthly)
                VALUES (
                    '{id}', 
                    COALESCE((SELECT Daily + {tempScore[id]} FROM Score WHERE Id = '{id}'), {tempScore[id]}),
                    COALESCE((SELECT Weekly + {tempScore[id]} FROM Score WHERE Id = '{id}'), {tempScore[id]}),
                    COALESCE((SELECT Monthly + {tempScore[id]} FROM Score WHERE Id = '{id}'), {tempScore[id]})
                )";


                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }

        }

        //начинает записывать статистику в кэш
        public static async Task StartStatUpdate(string id)
        {
            tempStat[id] = await GetUserStatById(id);
            tempScore[id] = 0;
        }


        /// <summary>
        /// Обновить статистику в кэше (не в базе данных)
        /// </summary>
        public static async Task UpdateUserStatistic(string id, string verb, bool isRight)
        {
            var stats = tempStat[id];
            var verbStat = stats.FirstOrDefault(item => item.Verb == verb);
            if (verbStat != null)
            {
                if (isRight)
                    verbStat.IncrementRightCount();
                else
                    verbStat.IncrementIncorrectCount();
            }
            else
                stats.Add(new StatItem(verb, isRight));

            if (isRight)
                tempScore[id] += 10; //+ StreakMultiplier(id);
        }

        static public async Task InitialiseUser(Message msg)
        {
            if (!IsExistUser(msg.Chat.Id))
            {
                allUsersId.Add(msg.Chat.Id);
                var date = DateTime.Now.ToShortDateString();
                using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    await conn.OpenAsync();
                    using (SqliteCommand cmd = new SqliteCommand())
                    {
                        cmd.CommandText = $"INSERT INTO[Users] ([Id], [Name], [Stat], [Date])" +
                            $" VALUES('{msg.Chat.Id}','{msg.From?.FirstName ?? "undefined"}', '', '{date}')";
                        cmd.Connection = conn;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

    }
}
