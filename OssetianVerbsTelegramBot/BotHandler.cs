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
            //else if (update.Type == UpdateType.CallbackQuery)
            //{
            //    await HandleCallbackQuery(update.CallbackQuery);
            //}
        }

        private async Task HandleMessage(Message message)
        {
            switch (message.Text)
            {
                case "/start":
                    break;
                case "📋 Определить тип":
                    var task = new TaskDefineType();
                    await SendTask1Question(message.Chat.Id, task);
                    break;
                default:
                    await SendMainMenu(message.Chat.Id);
                    break;
            }
        }

        //private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        //{
        //    switch (callbackQuery.Data)
        //    {
        //        case "option1":
        //            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Вы выбрали пункт 1");
        //            await _bot.SendMessage(callbackQuery.Message.Chat.Id, "Вы выбрали: Пункт 1");
        //            break;
        //        case "option2":
        //            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Вы выбрали пункт 2");
        //            await _bot.SendMessage(callbackQuery.Message.Chat.Id, "Вы выбрали: Пункт 2");
        //            break;
        //    }
        //}

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
                    new KeyboardButton("📞 Контакты"),
                    new KeyboardButton("ℹ️ О нас")
                },
                new[]
                {
                    new KeyboardButton("⚙️ Статистика")
                }
            });

            await _bot.SendMessage(
                chatId: chatId,
                text: "Навигация осуществляется с помощью меню👇",
                replyMarkup: keyboard
            );
        }


        private async Task SendTask1Question(long chatId, TaskDefineType task)
        {
            var verb = task.NextQuestion();
            while (verb != null)
            {
                var msg = $"""
                Глагол:
                ${verb.Inf}
                """;

                await _bot.SendMessage(
                   chatId: chatId,
                   text: "Навигация осуществляется с помощью меню👇",
                   replyMarkup: keyboard
               );
            }
        }


        private Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
