using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models
{
    public class StatItem
    {
        public string Verb { get; private set; }
        public int CorrectCount { get; private set; }
        public int IncorrectCount { get; private set; }
        public int TotalCount { get => CorrectCount+IncorrectCount; }
        public int Percent { get => (int)((decimal)CorrectCount / TotalCount * 100); }
        public StatItem(string verb, int correctCount , int incorrectCount ) 
        { 
            Verb = verb;
            CorrectCount = correctCount;
            IncorrectCount = incorrectCount;
        }
        public StatItem(string verb, bool isRight ) 
        { 
            Verb = verb;
            CorrectCount = isRight?1:0;
            IncorrectCount = isRight?0:1;
        }
        public void IncrementRightCount()
        {
            CorrectCount++;
        }
        public void IncrementIncorrectCount()
        {
            IncorrectCount++;
        }
        public override string ToString()
        {
            return $"{Verb} - {CorrectCount} из {TotalCount} ({Percent}%)";
        }
    }
}
