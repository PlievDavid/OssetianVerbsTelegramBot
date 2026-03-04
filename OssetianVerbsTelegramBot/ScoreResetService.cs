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
        public static async Task StartReset()
        {
            while (true)
            {
                var nextReset = DateTime.Now.Date.AddDays(1);
                var delay = nextReset - DateTime.Now;    

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                await Task.Delay(delay);

                await ResetDaily();

                if (nextReset.DayOfWeek == DayOfWeek.Monday)
                    await ResetWeekly();

                if (nextReset.Day == 1)
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
