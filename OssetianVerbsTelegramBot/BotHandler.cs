using OssetianVerbsTelegramBot.DeclinationTask;
using OssetianVerbsTelegramBot.DefineTypeTask;
using OssetianVerbsTelegramBot.Models;
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
                    await SendKeyboardLink(message);
                    await SendMainMenu(message.Chat.Id);
                    break;

                case "Глаголы":
                    await SendVerbMenu(message.Chat.Id);
                    break;

                case "Чат-бот":
                    await _bot.SendMessage(message.Chat.Id, "Данный раздел пока в разработке!⌛");
                    break;

                case "📋 Типы глагола":
                    ITaskHelper taskDefineType = new TaskDefineType(_bot, Sessions);
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, await DbVerbImport.GetRandomListVerb(message.Chat.Id), taskDefineType);
                    await taskDefineType.StartTask(message);
                    break;

                case "🖋️ Перевести":
                    ITaskHelper taskTranslate = new TaskTranslate(_bot, Sessions);
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, await DbVerbImport.GetRandomListVerb(message.Chat.Id), taskTranslate);
                    await taskTranslate.StartTask(message);
                    break;

                case "🛠️ Склонение":
                    ITaskHelper taskDeclination = new TaskDeclination(_bot, Sessions);
                    Sessions[message.Chat.Id] = new TestSession(message.Chat.Id, await DbVerbImport.GetRandomListVerb(message.Chat.Id), taskDeclination);
                    await taskDeclination.StartTask(message);
                    break;

                case "⚙️ Статистика":
                    await SendStatistics(message.Chat.Id);
                    break;
                case "💡 Справка":
                    await SendHelp(message.Chat.Id);
                    break;

                case "🔙 В главное меню":
                    await SendMainMenu(message.Chat.Id);
                    break;

                default:
                    if (Sessions[message.Chat.Id].Sentences.Count != 0)
                    {
                        var task = (TaskDeclination)Sessions[message.Chat.Id].Task;
                        await task.HandleMessageAnswer(message);
                        break;
                    }
                    await SendMainMenu(message.Chat.Id);
                    break;
            }
        }

        private async Task SendKeyboardLink(Message message)
        {
            string keyboardInformationString = """
                Чтобы пользоваться всеми функциями бота, тебе понадобится «Яндекс Клавиатура»!

                📲 Шаг 1: Установите «Яндекс Клавиатуру»
                Откройте App Store (iPhone) или Google Play (Android).

                Можете перейти по ссылкам снизу.

                ⚙️ Шаг 2: Включите клавиатуру в настройках
                Для iPhone:

                Откройте «Настройки» → «Основные» → «Клавиатура».

                Выберите «Клавиатуры» → «Добавить новую клавиатуру».

                Найдите «Яндекс Клавиатура» и добавьте.

                Для Android:

                После установки откройте приложение «Яндекс Клавиатура».

                Нажмите «Включить» и следуйте подсказкам.

                🌍 Шаг 3: Добавьте осетинскую раскладку
                Откройте любое приложение (WhatsApp, Notes).

                Нажмите на глобус (🌐) или пробел, чтобы переключиться на Яндекс Клавиатуру.

                Нажмите на иконку настроек (⚙️) → «Языки».

                Выберите «Ирон».
                """;

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(
                new InlineKeyboardButton[] {
                    new InlineKeyboardButton("Андроид", "https://play.google.com/store/apps/details?id=ru.yandex.androidkeyboard&hl=ru"),
                    new InlineKeyboardButton("IOS", "https://apps.apple.com/ru/app/яндекс-клавиатура/id1053139327")
                });

            await _bot.SendMessage(message.Chat.Id, keyboardInformationString, replyMarkup: markup);
        }

        private async Task SendStatistics(long id)
        {
            var list = await DbUser.GetUserStatById(id.ToString());
            string textStatistics = "Статистика правильных ответов: \n";
            foreach (var stat in list)
            {
                textStatistics += stat.ToString() + "\n";
            }
            await _bot.SendMessage(id, textStatistics);
        }
        private async Task SendHelp(long id)
        {
            var imageFile = File.Open("Images\\declinationRule.jpg", FileMode.Open);
            await _bot.SendPhoto(id, imageFile, caption:"Правило склонения глаголов в прошедшем времени.");
            var textVerbs = "Глаголы первого типа(переходные):\nИнфинитив - Морфема в прошедшем времени - Перевод\n";
            var firstTypeVerbs = await DbVerbImport.GetAllFirstTypeVerbs();
            var secondTypeVerbs = await DbVerbImport.GetAllSecondTypeVerbs();
            foreach (var verb in firstTypeVerbs)
            {
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";
            }
            await _bot.SendMessage(id, textVerbs);
            textVerbs = "Глаголы второго типа(непереходные):\nИнфинитив - Морфема в прошедшем времени - Перевод\n";
            foreach (var verb in secondTypeVerbs)
            {
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";
            }
            await _bot.SendMessage(id, textVerbs);
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
            var keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("Глаголы") },
                new[] { new KeyboardButton("Чат-бот") }, 
            }){
                ResizeKeyboard = true
            };

            await _bot.SendMessage(chatId: chatId,
                text: "Навигация осуществляется с помощью меню👇", replyMarkup: keyboard);
        }
        private async Task SendVerbMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("📋 Типы глагола"),
                    new KeyboardButton("🖋️ Перевести"),
                    new KeyboardButton("🛠️ Склонение")
                },
                new[]
                {
                    new KeyboardButton("⚙️ Статистика"),
                    new KeyboardButton("💡 Справка")
                },
                new[]
                {
                    new KeyboardButton("🔙 В главное меню")
                }
            })
            {
                ResizeKeyboard = true
            };

            await _bot.SendMessage(chatId: chatId,
                text: "Выберите задание в меню:", replyMarkup: keyboard);
        }





        private Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
