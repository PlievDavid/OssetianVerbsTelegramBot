using Microsoft.Data.Sqlite;
using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot
{
    public static class DbVerbImport
    {
        public static readonly string dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");
        public static List<Verb> AllVerbs { get; private set; }

        public static async Task InitializeVerbs() => AllVerbs = await GetAllVerbs();
        public static List<Verb> GetAllFirstTypeVerbs() => AllVerbs.Where(verb => verb.Type == 1).ToList();
        public static List<Verb> GetAllSecondTypeVerbs() => AllVerbs.Where(verb => verb.Type == 2).ToList();
        public static Verb GetRandomVerb() => AllVerbs[Random.Shared.Next(0, AllVerbs.Count)];

        static async Task<List<Verb>> GetAllVerbs()
        {
            var ans = new List<Verb>{ };
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT * FROM Verbs";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    ans.Add(new Verb(reader[0].ToString(), reader[1].ToString(), int.Parse(reader[2].ToString()), reader[3].ToString()));
                conn.Close();
            }
            return ans;
        }

        //Возвращает рандомные глаголы, часть которых будет из тех, в которых пользователь чаще ошибается
        public static async Task<List<Verb>> GetSmartRandomVerbs(string id, int count = 10)
        {
            var stat = await DbUser.GetUserStatById(id);
            var smartRandomVerbs = new HashSet<Verb>();
            var versbNeedToPractice = stat.Where(x => x.Percent <= 50).OrderBy(x => x.Percent);
            if (count >= AllVerbs.Count)
                return AllVerbs;

            //Сначала отбираем глаголы у которых низкий процент правильных ответов
            foreach (var verb in versbNeedToPractice)
            {
                if (Random.Shared.Next(0, 3) == 0)
                {
                    smartRandomVerbs.Add(AllVerbs.First(x => x.Inf == verb.Verb));
                    count--;

                    if (count <= 0)
                        return smartRandomVerbs.ToList();
                }
            }

            //Потом добираем оставшиеся глаголы
            while (count > 0)
            {
                var verb = AllVerbs[Random.Shared.Next(0, AllVerbs.Count)];
                if (smartRandomVerbs.Add(verb))
                    count--;
            }

            return smartRandomVerbs.ToList();

        }



    }
}
