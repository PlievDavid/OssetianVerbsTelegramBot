using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public class StatItem
    {
        public string Verb { get; private set; }
        public int RightCount { get; private set; }
        public int Count { get; private set; }
        public int Percent { get; private set; }
        public StatItem(string verb, string rightCount, string count) 
        { 
            Verb = verb;
            RightCount = int.Parse(rightCount);
            Count = int.Parse(count);
            Percent = (int)((decimal)RightCount / Count * 100);
        }
        public StatItem(string statSentence)
        {
            var temp = statSentence.Split(" - ");
            Verb = temp.First();
            var subTemp = temp.Last().Split(" из ");
            RightCount = int.Parse(subTemp.First());
            Count = int.Parse(subTemp.Last().Split('(').First());
            Percent = (int)((decimal)RightCount / Count * 100);
        }
        public void IncrementCount()
        {
            Count++;
            Percent = (int)((decimal)RightCount / Count * 100);
        }
        public void IncrementRightCount()
        {
            RightCount++;
            Count++;
            Percent = (int)((decimal)RightCount / Count * 100);
        }
        public override string ToString()
        {
            return $"{Verb} - {RightCount} из {Count}({Percent}%)";
        }
    }
}
