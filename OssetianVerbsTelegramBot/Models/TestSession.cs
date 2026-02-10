using OssetianVerbsTelegramBot.Tasks;
using OssetianVerbsTelegramBot.Tasks.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models
{
    public class TestSession
    {
        public long UserId { get; set; }
        public List<Verb> Verbs { get; private set; }
        public int CurrentIndex { get; set; } = 0;
        public int Score { get; set; } = 0;
        public ITaskStart Task { get; set; }
        public List<Sentence> Sentences { get; set; } = new List<Sentence>();

        public TestSession(long userId, List<Verb> verbs = null, ITaskStart task = null)
        {
            UserId = userId;
            Verbs = verbs;
            Task = task;
        }

    }
}
