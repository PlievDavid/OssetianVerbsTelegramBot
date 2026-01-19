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
        var botHandler = new BotHandler(GetBotToken());
        await botHandler.Start();

    }
    

    public static string GetBotToken()
    {
        string token = "";

        if (File.Exists(".env"))
        {
            Env.Load();

            token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            if (token != null)
            {
                return token;
            }
        }
        throw new Exception("Токен бота не найден!");
    }


    static public void InitialiseUser(Message msg)
    {
        if (!DbUser.IsExistUser(msg.Chat.Id.ToString()))
        {
            using (SqliteConnection conn = new("data source = ..\\..\\..\\VerbsDb.db"))
            {
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    string strSql = $"INSERT INTO[Users] ([Id], [Name], [Stat]) VALUES('{msg.Chat.Id}','{msg.From.FirstName}', '')";
                    cmd.CommandText = strSql;
                    cmd.Connection = conn;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
    }
    static public void FillVerbsDb(string path)
    {
        var sr = new StreamReader(path);
        using (SqliteConnection conn = new SqliteConnection("data source = ..\\..\\..\\VerbsDb.db"))
        {
            while (!sr.EndOfStream)
            {
                var temp = sr.ReadLine().Split(" - ");
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    string strSql = $"INSERT INTO[Verbs] ([Inf], [Past], [Type], [Trans]) VALUES('{temp[0].ToString()}','{temp[1].ToString()}', {int.Parse(temp[2].ToString())}, '{temp[3].ToString()}')";
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