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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                string sql = "SELECT id FROM users";
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
                string sql = $"SELECT verb, correct, incorrect FROM user_test_statistics WHERE user_id = {id} " +
                    $"ORDER BY correct DESC";
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
                    SELECT s.user_id, u.name, s.daily, s.weekly, s.monthly 
                    FROM user_scores s
                    JOIN users u ON s.user_id = u.id";
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
                        $@"INSERT INTO user_test_statistics (user_id, verb, correct, incorrect, total_count)
                        VALUES ('{id}', '{stat.Verb}', {stat.CorrectCount}, {stat.IncorrectCount}, {stat.TotalCount})
                        ON CONFLICT(user_id, verb) DO UPDATE SET
                            correct = excluded.correct,
                            incorrect = excluded.incorrect,
                            total_count = excluded.total_count";

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
                     $@"INSERT OR REPLACE INTO user_scores (user_id, daily, weekly, monthly)
                VALUES (
                    '{id}', 
                    COALESCE((SELECT daily + {tempScore[id]} FROM user_scores WHERE user_id = '{id}'), {tempScore[id]}),
                    COALESCE((SELECT weekly + {tempScore[id]} FROM user_scores WHERE user_id = '{id}'), {tempScore[id]}),
                    COALESCE((SELECT monthly + {tempScore[id]} FROM user_scores WHERE user_id = '{id}'), {tempScore[id]})
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
                    var sql = $"INSERT INTO users ([id], [name], [date])" +
                            $" VALUES('{msg.Chat.Id}','{msg.From?.FirstName ?? "undefined"}','{date}')";
                    using (SqliteCommand cmd = new SqliteCommand(sql,conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        static public async Task SaveVerbMistake(string verb, string mistake)
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                var sql = $@"INSERT INTO mistakes (verb, mistake, count) 
                            VALUES ('{verb}', '{mistake}', 1)
                            ON CONFLICT(verb, mistake) DO UPDATE SET 
                                count = count + 1;";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

    }
}
