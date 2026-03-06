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
        public long DailyScore { get; private set; }
        public long WeeklyScore { get; private set; }
        public long MonthlyScore { get; private set; }
        public RatingItem(long userId, string name, long dailyScore, long weeklyScore, long monthlyScore)
        {
            UserId = userId;
            Name = name;
            DailyScore = dailyScore;
            WeeklyScore = weeklyScore;
            MonthlyScore = monthlyScore;
        }

    }
}
