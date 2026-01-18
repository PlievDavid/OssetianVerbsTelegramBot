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
        private TaskDefineType _taskDefineType;

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
                    break;
                case "📋 Определить тип":
                    _taskDefineType = new TaskDefineType(message.Chat.Id);
                    await StartTask(_taskDefineType);
                    break;
                default:
                    await SendMainMenu(message.Chat.Id);
                    break;
            }
        }

        private async Task StartTask(TaskDefineType task)
        {
            SendNextQuestion(task);
        }

        private async Task SendNextQuestion(TaskDefineType task)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]{new InlineKeyboardButton("Первый тип", "firstType")},
                new[]{new InlineKeyboardButton("Второй тип", "secondType")}
            });
            var verb = task.NextQuestion();

            var msg = $"""
                Глагол:
                ${verb.Inf}
                """;

            await _bot.SendMessage(
               chatId: task.UserId,
               text: msg,
               replyMarkup: keyboard
           );

        }

        private async Task ProcessAnswer(CallbackQuery callbackQuery, int ans)
        {
            Console.WriteLine(callbackQuery.Message.Text);
            if (_taskDefineType.VerbIndex >= 9)
                SendMainMenu(callbackQuery.Message.Chat.Id);
            SendNextQuestion(_taskDefineType);
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            switch (callbackQuery.Data)
            {
                case "firstType":
                    await ProcessAnswer(callbackQuery,1); ;
                    break;
                case "secondType":
                    await ProcessAnswer(callbackQuery, 2); ;
                    break;
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
            });

            await _bot.SendMessage(
                chatId: chatId,
                text: "Навигация осуществляется с помощью меню👇",
                replyMarkup: keyboard
            );
        }





        private Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
