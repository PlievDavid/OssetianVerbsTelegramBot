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
                    var list = await DbVerbImport.GetRandomListVerb();
                    foreach (var item in list)
                    {
                        Console.WriteLine(item.Inf);
                    }
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, list);
                    await SendVerb(message.Chat.Id);
                    break;
                case "🖋️ Перевести":
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, DbVerbImport.GetRandomListVerb());
                    TaskTranslate taskTranslate = new TaskTranslate(_bot, Sessions);
                    taskTranslate.StartTranslateTask(message);
                    break;
                case "🖋️ Статистика":
                    await SendStatistics(message.Chat.Id);
                    break;
                default:
                    await SendMainMenu(message.Chat.Id);
                    break;
            }
        }

        private async Task SendStatistics(long id)
        {
            var list = DbUser.GetUserStatById(id.ToString());
            string textStatistics = "Статистика ошибок: \n";
            foreach (var stat in list)
            {
                textStatistics += stat.ToString() + "\n";
            }
            await _bot.SendMessage(id, textStatistics);
        }

        private async Task SendVerb(long chatId)
        {
            var session = Sessions[chatId];
            var verb = session.Verbs[session.CurrentIndex];
            Console.WriteLine(session.CurrentIndex);
            foreach (var item in session.Verbs)
            {
                Console.Write(item.Inf+" ");
            }
            Console.WriteLine(session.CurrentIndex);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Тип 1", "answer_1"),
                    InlineKeyboardButton.WithCallbackData("Тип 2", "answer_2")
                }
            });

            Console.WriteLine($"{verb.Inf} {verb.Type}");
            await _bot.SendMessage(
                chatId,
                $"Слово {session.CurrentIndex + 1}/10:\n\n{verb.Inf}",
                replyMarkup: keyboard
            );
        }


        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id);

            if (callbackQuery.Data.StartsWith("answer_"))
            {
                var chatId = callbackQuery.Message.Chat.Id;
                int answer = int.Parse(callbackQuery.Data.Split('_')[1]);
                var session = Sessions[chatId];
                var verb = session.Verbs[session.CurrentIndex];

                if (answer == verb.Type)
                {
                    session.Score++;
                    await _bot.SendMessage(
                       chatId,
                       $"Правильный ответ!✅"
                   );
                }
                else
                {
                    await _bot.SendMessage(
                       chatId,
                       $"Неправильный ответ!❌"
                   );
                    await DbUser.UpdateUserStat(chatId.ToString(), verb.Inf);
                }

                    session.CurrentIndex++;

                if (session.CurrentIndex < session.Verbs.Count)
                {
                    await SendVerb(chatId);
                }
                else
                {
                    await _bot.SendMessage(
                        chatId,
                        $"Тест завершён!\nРезультат: {session.Score}/10"
                    );

                    Sessions.Remove(chatId);
                }
            }
            else
            {
                TaskTranslate taskTranslate = new TaskTranslate(_bot, Sessions);
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
