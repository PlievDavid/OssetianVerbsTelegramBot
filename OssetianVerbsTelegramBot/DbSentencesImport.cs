using Microsoft.Data.Sqlite;
using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    internal class DbSentencesImport
    {
        static Random rnd = new Random();
        public static async Task<List<Sentence>> GetAllSentences()
        {
            var ans = new List<Sentence> { };
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = "SELECT * FROM Sentences";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ans.Add(new  Sentence(reader[1].ToString(), reader[2].ToString(), reader[3].ToString()));
                }
                conn.Close();
            }
            return ans;
        }
        public static async Task<Sentence> GetRandomSentence()
        {
            var sentences = await GetAllSentences();
            return sentences[rnd.Next(0, sentences.Count)];
        }
        public static async Task<Sentence> GetRandomSentenceByVerbInf(string verbInf)
        {
            var sentences = await GetAllSentences();
            return sentences.Where(item => item.VerbInf == verbInf).ToList()[rnd.Next(0, 6)];
        }
        public static async Task<List<Sentence>> GetRandomListSentence(int count = 10)
        {
            var all = await GetAllSentences();
            var allCount = all.Count;
            var list = new List<Sentence>();
            for (int i = 0; i < count; i++)
            {
                var sentence = await GetRandomSentence();
                if (list.Any(x => x.VerbInf == sentence.VerbInf))
                {
                    if (count > allCount)
                        return list;
                    else
                        i--;
                }
                else
                    list.Add(sentence);
            }
            return list;
        }
        public static async Task<List<Sentence>> GetRandomListSentenceByListVerb(List<Verb> verbs)
        {
            var list = new List<Sentence>();
            foreach(var verb in verbs)
            {
                list.Add(await GetRandomSentenceByVerbInf(verb.Inf));
            }
            return list;
        }

    }
}
