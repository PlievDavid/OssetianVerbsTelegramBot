using Microsoft.Data.Sqlite;
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
        public static List<Verb> GetAllVerbs()
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
        public static List<Verb> GetAllFirstTypeVerbs()
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
        public static List<Verb> GetAllSecondTypeVerbs()
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
        public static Verb GetRandomVerb()
        {
            var verbs = GetAllVerbs();
            return verbs[rnd.Next(0, verbs.Count)];
        }
        public static Verb GetRandomFirstTypeVerb()
        {
            var verbs = GetAllFirstTypeVerbs();
            return verbs[rnd.Next(0, verbs.Count)];
        }
        public static Verb GetRandomSecondTypeVerb()
        {
            var verbs = GetAllSecondTypeVerbs();
            return verbs[rnd.Next(0, verbs.Count)];
        }
    }
}
