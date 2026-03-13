using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models
{
    public class Verb
    {
        public string Infinitive { get; private set; }
        public string Past { get; private set; }
        public int Type { get; private set; }
        public string Translation { get; private set; }
        public Verb(string inf, string past, int type, string trans)
        {
            Infinitive = inf;
            Past = past;
            Type = type;
            Translation = trans;
        }
    }
}
