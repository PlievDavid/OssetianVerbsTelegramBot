using DotNetEnv;
using Microsoft.Data.Sqlite;
using OssetianVerbsTelegramBot;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

internal class Program
{
    static async Task Main(string[] args)
    {
#if WINDOWS
        var botHandler = new BotHandler(EnvironmentManager.GetTestBotToken());
        await botHandler.Start();
#else
        var botHandler = new BotHandler(EnvironmentManager.GetBotTokenhh());
        await botHandler.Start();
#endif
    }



    static public void FillVerbsDb(string path)
    {
        var sr = new StreamReader(path);
        var dbPath = Path.Combine(AppContext.BaseDirectory, "VerbsDb.db");
        using (SqliteConnection conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            while (!sr.EndOfStream)
            {
                var id = Guid.NewGuid();
                var temp = sr.ReadLine().Split(" - ");
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    string strSql = $"INSERT INTO[Sentences] ([Id], [Russian], [Ossetian], [Verb]) VALUES('{id}','{temp[0].ToString()}', '{temp[1].ToString()}', '{temp[2].ToString()}')";
                    cmd.CommandText = strSql;
                    cmd.Connection = conn;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
    }
}