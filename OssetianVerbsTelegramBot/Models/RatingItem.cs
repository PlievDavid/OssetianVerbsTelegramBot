using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models
{
    public class RatingItem
    {
        public long UserId { get; private set; }
        public string Name { get; private set; }
        public int DailyScore { get; private set; }
        public int WeeklyScore { get; private set; }
        public int MonthlyScore { get; private set; }
        public RatingItem(long userId, string name, int dailyScore, int weeklyScore, int monthlyScore)
        {
            UserId = userId;
            Name = name;
            DailyScore = dailyScore;
            WeeklyScore = weeklyScore;
            MonthlyScore = monthlyScore;
        }

    }
}
