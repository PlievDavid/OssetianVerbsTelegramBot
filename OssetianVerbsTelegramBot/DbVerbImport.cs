using Microsoft.Data.Sqlite;
using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public static class DbVerbImport
    {
        static Random rnd = new Random();
        public static async Task<List<Verb>> GetAllVerbs()
        {
            var ans = new List<Verb> { };
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = "SELECT * FROM Verbs";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ans.Add(new Verb(reader[0].ToString(), reader[1].ToString(), int.Parse(reader[2].ToString()), reader[3].ToString()));
                }
                conn.Close();
            }
            return ans;
        }
        public static async Task<List<Verb>> GetAllFirstTypeVerbs()
        {
            var ans = new List<Verb> { };
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = "SELECT * FROM Verbs WHERE Type = 1";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ans.Add(new Verb(reader[0].ToString(), reader[1].ToString(), int.Parse(reader[2].ToString()), reader[3].ToString()));
                }
                conn.Close();
            }
            return ans;
        }
        public static async Task<List<Verb>> GetAllSecondTypeVerbs()
        {
            var ans = new List<Verb> { };
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = "SELECT * FROM Verbs WHERE Type = 2";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ans.Add(new Verb(reader[0].ToString(), reader[1].ToString(), int.Parse(reader[2].ToString()), reader[3].ToString()));
                }
                conn.Close();
            }
            return ans;
        }
        public static async Task<Verb> GetRandomVerb()
        {
            var verbs = await GetAllVerbs();
            return verbs[rnd.Next(0, verbs.Count)];
        }
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
        public static async Task<List<Verb>> GetRandomListVerb(long id, int count = 10)
        {
            var all = await GetAllVerbs();
            var allCount = all.Count;
            var list = new List<Verb>();
            for (int i = 0; i < count; i++)
            {
                var verb = await GetSmartRandomVerb(id.ToString());
                if (list.Any(x => x.Inf == verb.Inf))
                {
                    if (count > allCount)
                        return list;
                    else
                        i--;
                }
                else
                    list.Add(verb);
            }
            return list;
        }
    }
}
