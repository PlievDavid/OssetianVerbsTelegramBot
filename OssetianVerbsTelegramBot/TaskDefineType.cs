using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public class TaskDefineType
    {
        public long UserId { get; }
        public Verb Verb { get; set; }
        public int VerbIndex { get; private set; }


        private readonly List<Verb> _verbs;
        public TaskDefineType(long userId)
        {
            UserId = userId;
            _verbs = DbVerbImport.GetRandomListVerb();
        }

        public Verb NextQuestion()
        {
            var verb = _verbs.FirstOrDefault();
            if(verb != null)
            {
                _verbs.Remove(verb);
                IncreaseVerbIndex();
            }
            Verb = verb;
            return verb;
            
        }

        public void IncreaseVerbIndex()
        {
            VerbIndex++;
        }


    }
}
