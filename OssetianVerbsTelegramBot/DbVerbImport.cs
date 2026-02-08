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
        private static readonly string dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");
        public static List<Verb>? AllVerbs { get; private set; }

        public static async Task InitializeVerbs() => AllVerbs = await GetAllVerbs();
        public static List<Verb> GetAllFirstTypeVerbs() => AllVerbs.Where(verb => verb.Type == 1).ToList();
        public static List<Verb> GetAllSecondTypeVerbs() => AllVerbs.Where(verb => verb.Type == 2).ToList();
        public static Verb GetRandomVerb() => AllVerbs[Random.Shared.Next(0,AllVerbs.Count)];
        
        public static List<Verb> GetRandomListVerb(int count = 10)
        {
            var allCount = AllVerbs.Count();
            var list = new List<Verb>();
            for (int i = 0; i < count; i++)
            {
                var verb = AllVerbs[Random.Shared.Next(0,allCount)];
                if (list.Any(x => x.Inf == verb.Inf))
                    if (count > allCount) return list;
                    else i--;
                else
                    list.Add(verb);
            }
            return list;
        }

        static async Task<List<Verb>> GetAllVerbs()
        {
            var ans = new List<Verb> { };
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



        /*
         public static async Task<Verb> GetSmartRandomVerb(string id)
        {
            var stat = await DbUser.GetUserStatById(id);
            var verbs = await GetAllVerbs();
            var ans = verbs[rnd.Next(0, verbs.Count)];
            if (stat.Count < verbs.Count)
            {
                while (stat.FirstOrDefault(item => item.Verb == ans.Inf) != null)
                {
                    ans = verbs[rnd.Next(0, verbs.Count)];
                }
            }
            else
            {
                var chance = rnd.Next(0, 3);
                if (chance < 2)
                {
                    if (stat.FirstOrDefault(item => item.Percent < 50) != null)
                    {
                        while (stat.FirstOrDefault(item => item.Verb == ans.Inf).Percent >= 50)
                        {
                            ans = verbs[rnd.Next(0, verbs.Count)];
                        }
                    }
                }
                else
                {
                    if (stat.FirstOrDefault(item => item.Percent >= 50) != null)
                    {
                        while (stat.FirstOrDefault(item => item.Verb == ans.Inf).Percent < 50)
                        {
                            ans = verbs[rnd.Next(0, verbs.Count)];
                        }
                    }
                }
            }
            return ans;
        }
        */

    }
}
