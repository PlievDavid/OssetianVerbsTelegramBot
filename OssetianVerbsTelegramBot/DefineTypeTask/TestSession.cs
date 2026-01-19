using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.DefineTypeTask
{
    class TestSession
    {
        public long UserId { get; set; }
        public List<Verb> Verbs { get; private set; }
        public int CurrentIndex { get; set; } = 0;
        public int Score { get; set; } = 0;

        public int CurrentIndexTranslateTask { get; set; } = 0;
        public int ScoreTranslateTask { get; set; } = 0;

        public TestSession(long userId, List<Verb> verbs)
        {
            UserId = userId;
            Verbs = verbs;
        }

    }
}
