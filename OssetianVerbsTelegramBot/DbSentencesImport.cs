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

        private static readonly string dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");
        public static List<Sentence> AllSentences { get; private set; } = [];


        public static async Task InitializeSentences() => AllSentences = await GetAllSentences();
        public static Sentence GetRandomSentence() => AllSentences[Random.Shared.Next(0, AllSentences.Count)];
        public static Sentence GetRandomSentenceByVerbInf(string verbInf)
        {
            var sorted = AllSentences.Where(x => x.VerbInf == verbInf).ToList();
            return sorted[Random.Shared.Next(0, sorted.Count)];
        }
        public static List<Sentence> GetRandomListSentence(int count = 10)
        {
            var allCount = AllSentences.Count;
            var list = new List<Sentence>();
            for (int i = 0; i < count; i++)
            {
                var sentence = GetRandomSentence();
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
        public static List<Sentence> GetRandomListSentenceByListVerb(List<Verb> verbs)
        {
            List<Sentence> list = new();
            foreach (var verb in verbs)
            {
                list.Add(GetRandomSentenceByVerbInf(verb.Inf));
            }
            return list;
        }

        public static async Task<List<Sentence>> GetAllSentences()
        {
            List<Sentence> ans = new();
            using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                using (SqliteCommand command = new SqliteCommand("SELECT * FROM Sentences", conn))
                {
                    SqliteDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        ans.Add(new Sentence(reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                    }
                }
            }
            return ans;
        }

    }
}
