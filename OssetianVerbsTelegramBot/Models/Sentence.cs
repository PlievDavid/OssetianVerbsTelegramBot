using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models
{
    public class Sentence
    {
        public string Russian { get; private set; }
        public string Ossetian { get; private set; }
        public string VerbInf { get; private set; }
        public Sentence(string russian,string ossetian, string verbInf) 
        { 
            Russian = russian;  
            Ossetian = ossetian;
            VerbInf = verbInf;
        }
    }
}
