using OssetianVerbsTelegramBot.DefineTypeTask;
using OssetianVerbsTelegramBot.TranslateTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot
{
    public class BotHandler
    {
        private readonly TelegramBotClient _bot;
        private static Dictionary<long, TestSession> Sessions = new();

        public BotHandler(string token)
        {
            _bot = new TelegramBotClient(token);
        }

        public async Task Start()
        {
            _bot.StartReceiving(UpdateHandler, ErrorHandler);
            Console.WriteLine("Бот запущен!");

            await Task.Delay(-1);
        }

        private async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                if (message?.Text != null)
                {
                    await HandleMessage(message);
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(update.CallbackQuery);
            }
        }

        private async Task HandleMessage(Message message)
        {
            switch (message.Text)
            {
                case "/start":
                    await DbUser.InitialiseUser(message);
                    await SendMainMenu(message.Chat.Id);
                    break;

                case "📋 Определить тип":
                    ITaskHelper taskDefineType = new TaskDefineType(_bot, Sessions);
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, await DbVerbImport.GetRandomListVerb(),taskDefineType);
                    await taskDefineType.StartTask(message);
                    break;

                case "🖋️ Перевести":
                    ITaskHelper taskTranslate = new TaskTranslate(_bot, Sessions);
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, await DbVerbImport.GetRandomListVerb(), taskTranslate);
                    await taskTranslate.StartTask(message);
                    break;

                case "⚙️ Статистика":
                    await SendStatistics(message.Chat.Id);
                    break;
                default:
                    await SendMainMenu(message.Chat.Id);
                    break;
            }
        }

        private async Task SendStatistics(long id)
        {
            var list = await DbUser.GetUserStatById(id.ToString());
            string textStatistics = "Статистика ошибок: \n";
            foreach (var stat in list)
            {
                textStatistics += stat.ToString() + "\n";
            }
            await _bot.SendMessage(id, textStatistics);
        }



        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id);

            if (callbackQuery.Data.StartsWith("answer_"))
            {
                var task = Sessions[callbackQuery.Message.Chat.Id].Task;
                await task.HandleCallbackQuery(callbackQuery);
            }

            else
            {
                var taskTranslate = Sessions[callbackQuery.Message.Chat.Id].Task;
                await taskTranslate.HandleCallbackQuery(callbackQuery);
            }
        }


        private async Task SendMainMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            { 
                new[]
                {
                    new KeyboardButton("📋 Определить тип"),
                    new KeyboardButton("🖋️ Перевести")
                },
                new[]
                {
                    new KeyboardButton("⚙️ Статистика")
                }
            })
            {
                ResizeKeyboard = true
            };

            await _bot.SendMessage(chatId: chatId,
                text: "Навигация осуществляется с помощью меню👇",  replyMarkup: keyboard  );
        }





        private Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
