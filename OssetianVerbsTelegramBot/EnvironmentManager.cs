using DotNetEnv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    static class EnvironmentManager
    {
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
        public static string GetTestBotToken()
        {
            string token = "";
            if (File.Exists(".env"))
            {
                Env.Load();
                token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN_TEST");
                if (token != null)
                {
                    return token;
                }
            }
            throw new Exception("Токен бота не найден!");
        }

        public static string GetYandexGptKey()
        {
            string token = "";
            if (File.Exists(".env"))
            {
                Env.Load();
                token = Environment.GetEnvironmentVariable("YANDEX_API_KEY");
                if (token != null)
                {
                    return token;
                }
            }
            throw new Exception("Токен бота не найден!");
        }

        public static string GetYandexProjectId()
        {
            string token = "";
            if (File.Exists(".env"))
            {
                Env.Load();
                token = Environment.GetEnvironmentVariable("YANDEX_PROJECT_ID");
                if (token != null)
                {
                    return token;
                }
            }
            throw new Exception("Токен бота не найден!");
        }
    }
}
