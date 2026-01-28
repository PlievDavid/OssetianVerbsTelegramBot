using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.DefineTypeTask
{
    public class TestSession
    {

        public long UserId { get; set; }
        public List<Verb> Verbs { get; private set; }
        public int CurrentIndex { get; set; } = 0;
        public int Score { get; set; } = 0;
        public ITaskHelper Task { get; set; }

        public int CurrentIndexTranslateTask { get; set; } = 0;
        public int ScoreTranslateTask { get; set; } = 0;

        public int CurrentIndexDeclinationTask { get; set; } = 0;
        public int ScoreDeclinationTask { get; set; } = 0;
        public List<Sentence> Sentences { get; set; } = new List<Sentence>();

        public TestSession(long userId, List<Verb> verbs, ITaskHelper task)
        {
            UserId = userId;
            Verbs = verbs;
            Task = task;
        }

    }
}
