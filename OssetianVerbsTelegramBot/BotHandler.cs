using OssetianVerbsTelegramBot.ApiClients.Yandex;
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
        private static Dictionary<long, TestSession> taskSessions = new();
        private static Dictionary<long, ChatSession> chatSessions = new();
        YandexTranslateClient yandexTranslateClient = new YandexTranslateClient(EnvironmentManager.GetYandexGptKey(), EnvironmentManager.GetYandexProjectId());
        private YandexGptClient yandexGptClient = new YandexGptClient(EnvironmentManager.GetYandexGptKey(), EnvironmentManager.GetYandexProjectId());
        private Dictionary<long, int[]> helpMessages = new();

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
            var chatId = message.Chat.Id;

            if (!chatSessions.ContainsKey(chatId))
            {
                chatSessions[message.Chat.Id] = new ChatSession(chatId, false);
            }

            if (helpMessages.ContainsKey(chatId))
            {
                var messages = helpMessages[chatId];
                await _bot.DeleteMessages(chatId, messages);
                helpMessages.Remove(chatId);
            }


            try
            {
                switch (message.Text)
                {
                    case "/start":
                        await DbUser.InitialiseUser(message);
                        await SendKeyboardLink(message);
                        await SendMainMenu(chatId);
                        break;

                    case "📝 Глаголы":
                        await SendVerbMenu(chatId);
                        chatSessions[message.Chat.Id].IsGptMode = false;
                        break;

                    case "🤖 Чат-бот (Beta)":
                        chatSessions[message.Chat.Id].IsGptMode = true;
                        await _bot.SendMessage(message.Chat.Id, "<b>Режим чат-бота включен</b> ✅", parseMode: ParseMode.Html);
                        break;

                    case "📋 Типы глагола":
                        ITask taskDefineType = new TaskDefineType(_bot, taskSessions);
                        taskSessions[chatId] = new TestSession(chatId, await DbVerbImport.GetRandomListVerb(chatId), taskDefineType);
                        await taskDefineType.StartTask(message);
                        break;

                    case "🖋️ Перевести":
                        ITask taskTranslate = new TaskTranslate(_bot, taskSessions);
                        taskSessions[chatId] = new TestSession(chatId, await DbVerbImport.GetRandomListVerb(chatId), taskTranslate);
                        await taskTranslate.StartTask(message);
                        break;

                    case "🛠️ Склонение":
                        ITask taskDeclination = new TaskDeclination(_bot, taskSessions);
                        taskSessions[chatId] = new TestSession(chatId, await DbVerbImport.GetRandomListVerb(chatId), taskDeclination);
                        await taskDeclination.StartTask(message);
                        break;

                    case "⚙️ Статистика":
                        await SendStatistics(chatId);
                        break;
                    case "💡 Справка":
                        var messages = await SendHelp(chatId);
                        helpMessages[chatId] = messages;
                        break;

                    case "🔙 В главное меню":
                        await SendMainMenu(chatId);
                        break;

                    default:
                        if (chatSessions[message.Chat.Id].IsGptMode)
                        {
                            Console.WriteLine("User(" + message.Chat.Id + " - " + message.From.Username + "): " + message.Text);

                            var loadSmile = await _bot.SendMessage(message.Chat.Id, "⏳");

                            var ruMessage = await yandexTranslateClient.TranslateTextAsync(message.Text, "os", "ru");

                            chatSessions[chatId].ChatHistory += $"User: {ruMessage}\n";

                            var response = await yandexGptClient.SendRequestAsync(chatSessions[chatId].ChatHistory);

                            Console.WriteLine("GPT: " + response);

                            chatSessions[chatId].ChatHistory += $"GPT: {response}\n";

                            await _bot.SendMessage(message.Chat.Id, $"<b>{await yandexTranslateClient.TranslateTextAsync(response, "ru", "os")}</b>", parseMode: ParseMode.Html);

                            await _bot.DeleteMessage(message.Chat.Id, loadSmile.Id);
                        }
                        else
                        {
                            if (taskSessions[message.Chat.Id].Sentences.Count != 0)
                            {
                                var task = (TaskDeclination)taskSessions[message.Chat.Id].Task;
                                await task.HandleMessageAnswer(message);
                                break;
                            }
                            await SendMainMenu(message.Chat.Id);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task SendKeyboardLink(Message message)
        {
            string keyboardInformationString = """
                Чтобы пользоваться всеми функциями бота, вам понадобится «Яндекс Клавиатура»
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


        private async Task<int[]> SendHelp(long id)
        {
            var imageFile = File.Open(Path.Combine("Images","declinationRule.jpg"), FileMode.Open);
            var photoMessage = await _bot.SendPhoto(id, imageFile, caption: "Правило склонения глаголов в прошедшем времени.");
            var textVerbs = "<b>Переходные глаголы:</b>\n<i>Инфинитив - Морфема в прошедшем времени - Перевод<i>\n";
            var firstTypeVerbs = await DbVerbImport.GetAllFirstTypeVerbs();
            var secondTypeVerbs = await DbVerbImport.GetAllSecondTypeVerbs();
            foreach (var verb in firstTypeVerbs)
            {
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";
            }
            var firstTypeMessage = await _bot.SendMessage(id, textVerbs);
            textVerbs = "<b>Непереходные глаголы:</b>\n<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>\n";
            foreach (var verb in secondTypeVerbs)
            {
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";
            }
            var secondTypeMessage = await _bot.SendMessage(id, textVerbs, parseMode: ParseMode.Html);
            return new[] { photoMessage.MessageId, firstTypeMessage.MessageId, secondTypeMessage.MessageId, photoMessage.MessageId - 1 };
        }


        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id);

            if (callbackQuery.Data.StartsWith("answer_"))
            {
                var task = taskSessions[callbackQuery.Message.Chat.Id].Task;
                await task.HandleCallbackQuery(callbackQuery);
            }

            else
            {
                var taskTranslate = taskSessions[callbackQuery.Message.Chat.Id].Task;
                await taskTranslate.HandleCallbackQuery(callbackQuery);
            }
        }


        private async Task SendMainMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("📝 Глаголы") },
                new[] { new KeyboardButton("🤖 Чат-бот (Beta)") },
            })
            {
                ResizeKeyboard = true
            };

            await _bot.SendMessage(chatId: chatId,
                text: "<b> Навигация осуществляется с помощью меню</b> 👇", replyMarkup: keyboard, parseMode: ParseMode.Html);
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
                text: "<b>Выберите задание в меню:</b>", replyMarkup: keyboard, parseMode: ParseMode.Html);
        }





        private Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
