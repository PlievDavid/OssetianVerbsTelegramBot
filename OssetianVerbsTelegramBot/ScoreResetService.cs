using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public static class ScoreResetService
    {
        public static void Start()
        {
            _ = Task.Run(RunAsync);
        }

        private static async Task RunAsync()
        {
            while (true)
            {
                TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                DateTime nowMoscow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);
                DateTime tomorrowMoscow = nowMoscow.Date.AddDays(1);
                TimeSpan delay = tomorrowMoscow - nowMoscow;

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                await Task.Delay(delay);

                await ResetDaily();

                if (tomorrowMoscow.DayOfWeek == DayOfWeek.Monday)
                    await ResetWeekly();

                if (tomorrowMoscow.Day == 1)
                    await ResetMonthly();
            }
        }

        private static async Task ResetDaily()
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={DbUser.dbPath}"))
            {
                await conn.OpenAsync();
                using (var cmd = new SqliteCommand("UPDATE Users SET DailyScore = 0", conn))
                    await cmd.ExecuteNonQueryAsync();
            }


        }
        public static async Task ResetWeekly()
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={DbUser.dbPath}"))
            {
                await conn.OpenAsync();
                using (var cmd = new SqliteCommand("UPDATE Users SET WeeklyScore = 0", conn))
                    await cmd.ExecuteNonQueryAsync();
            }

        }
        public static async Task ResetMonthly()
        {
            using (SqliteConnection conn = new SqliteConnection($"Data Source={DbUser.dbPath}"))
            {
                await conn.OpenAsync();
                using (var cmd = new SqliteCommand("UPDATE Users SET MonthlyScore = 0", conn))
                    await cmd.ExecuteNonQueryAsync();
            }

        }

    }
}
