using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public static class DbUser
    {
        public static bool IsExistUser(string id)
        {
            var ans = false;
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = $"SELECT * FROM Users WHERE Id = '{id}'";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                    ans = true;
                conn.Close();
            }
            return ans;
        }
        public static List<StatItem> GetUserStatById(string id)
        {
            var ans = new List<StatItem> {  };
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                conn.Open();
                string sql = $"SELECT * FROM Users WHERE Id = '{id}'";
                SqliteCommand command = new SqliteCommand(sql, conn);
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var temp = reader[2].ToString().Split("\n");
                    foreach (var item in temp)
                    {
                        var subTemp = item.Split();
                        ans.Add(new StatItem(subTemp[0],subTemp[1]));
                    }
                }
                conn.Close();
            }
            return ans;
        }
        public static void FillStatWithList(List<StatItem> list, string id)
        {
            list = list.OrderByDescending(item => item.ErrorCount).ToList();
            var ans = "";
            foreach(var item in list)
            {
                ans += item.ToString() + "\n";
            }
            using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
            {
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    conn.Open();
                    string sql = $"Update Users Set Stat = '{ans}' WHERE Id = '{id}'";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        public static void UpdateUserStat(string id, string verb)
        {
            var stat = GetUserStatById(id);
            if (stat.FirstOrDefault(item => item.Verb == verb) == null)
            {
                stat.Add(new StatItem(verb, "1"));
            }
            else
            {
                stat.First(item => item.Verb == verb).Increment();
            }
            FillStatWithList(stat, id);
        }
    }
}
