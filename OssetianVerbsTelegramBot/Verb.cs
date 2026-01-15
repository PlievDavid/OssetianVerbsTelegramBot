using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public class Verb
    {
        public string Inf { get; private set; }
        public string Past { get; private set; }
        public int Type { get; private set; }
        public string Trans { get; private set; }
        public Verb(string inf, string past, int type, string trans)
        {
            Inf = inf;
            Past = past;
            Type = type;
            Trans = trans;
        }
    }
}
