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
        public int ErrorCount { get; private set; }
        public StatItem(string verb, string errorCount) 
        { 
            Verb = verb;
            ErrorCount = int.Parse(errorCount);
        }
        public void Increment()
        {
            ErrorCount++;
        }
        public override string ToString()
        {
            return Verb + " " + ErrorCount;
        }
    }
}
