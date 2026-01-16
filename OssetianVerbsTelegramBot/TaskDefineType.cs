using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public class TaskDefineType
    {
        private readonly HashSet<Verb> _verbs;
        public TaskDefineType()
        {
            _verbs = new HashSet<Verb>();
            while (_verbs.Count != 10)
            {
                _verbs.Add(DbVerbImport.GetRandomVerb());
            }
        }

        public Verb NextQuestion()
        {
            var verb = _verbs.FirstOrDefault();
            if(verb != null)
            {
                _verbs.Remove(verb);
            }
            return verb;
            
        }


    }
}
