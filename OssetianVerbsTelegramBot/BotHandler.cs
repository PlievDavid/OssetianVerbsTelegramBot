using OssetianVerbsTelegramBot.ApiClients.Yandex;
using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks;
using OssetianVerbsTelegramBot.Tasks.Interface;
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
        private string[] admins = { "946534275" };

        public BotHandler(string token)
        {
            _bot = new TelegramBotClient(token);
        }

        public async Task Start()
        {
            await DbVerbImport.InitializeVerbs();
            await DbSentencesImport.InitializeSentences();
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
            try
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

                switch (message.Text)
                {
                    case "/start":
                        await DbUser.InitialiseUser(message);
                        await SendKeyboardLink(message);
                        await SendMainMenu(chatId);
                        break;

                    case "📝 Глаголы":
                        await SendVerbMenu(chatId);
                        chatSessions[chatId].IsGptMode = false;
                        break;

                    case "🤖 Чат-бот (Beta)":
                        chatSessions[chatId].IsGptMode = true;
                        await _bot.SendMessage(chatId, "<b>Режим чат-бота включен</b> ✅", parseMode: ParseMode.Html);
                        break;

                    case "📋 Тип глагола":
                        ITaskStart taskDefineType = new TaskDefineType(_bot, taskSessions);
                        await taskDefineType.StartTask(message);
                        break;

                    case "🖋️ Перевести":
                        ITaskStart taskTranslate = new TaskTranslate(_bot, taskSessions);
                        await taskTranslate.StartTask(message);
                        break;

                    case "🛠️ Спряжение":
                        ITaskStart taskDeclination = new TaskDeclination(_bot, taskSessions);
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
                        if (chatSessions[chatId].IsGptMode)
                        {
                            Console.WriteLine("User(" + chatId + " - " + message.From.Username + "): " + message.Text);

                            var loadSmile = await _bot.SendSticker(chatId, sticker: "CAACAgUAAxkBAAEVynlphwOBCtgySn0lY4gZRq60cHjnFgACFwsAAnpH2FSrntiSYBUw7ToE");

                            var ruMessage = await yandexTranslateClient.TranslateTextAsync(message.Text, "os", "ru");

                            chatSessions[chatId].AddHistory($"User: {ruMessage}");

                            var response = await yandexGptClient.SendRequestAsync(chatSessions[chatId].ChatHistory);

                            Console.WriteLine("GPT: " + response);

                            chatSessions[chatId].AddHistory($"GPT: {response}");

                            await _bot.SendMessage(chatId, $"<b>{await yandexTranslateClient.TranslateTextAsync(response, "ru", "os")}</b>", parseMode: ParseMode.Html);


                            await _bot.DeleteMessage(chatId, loadSmile.Id);
                        }
                        else
                        {
                            if (taskSessions.ContainsKey(chatId))
                            {
                                var task = taskSessions[chatId].Task;
                                if (taskSessions[chatId].Sentences.Count != 0 && task is IMessageTask msgTask)
                                    await msgTask.HandleMessageAnswer(message);
                            }
                            else
                            {
                                await SendMainMenu(chatId);
                            }
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
            var firstTypeVerbs = DbVerbImport.GetAllFirstTypeVerbs();
            var secondTypeVerbs = DbVerbImport.GetAllSecondTypeVerbs();
            var imageFile = File.Open(Path.Combine("Images", "declinationRule.jpg"), FileMode.Open);

            var photoMessage = await _bot.SendPhoto(id, imageFile, caption: "Правило спряжения глаголов в прошедшем времени.");

            var textVerbs = "<b>Переходные глаголы:</b>\n<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>\n";
            foreach (var verb in firstTypeVerbs)
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";

            var firstTypeMessage = await _bot.SendMessage(id, textVerbs, parseMode: ParseMode.Html);

            textVerbs = "<b>Непереходные глаголы:</b>\n<i>Инфинитив - Морфема в прошедшем времени - Перевод</i>\n";
            foreach (var verb in secondTypeVerbs)
                textVerbs += $"{verb.Inf} - {verb.Past} - {verb.Trans}\n";

            var secondTypeMessage = await _bot.SendMessage(id, textVerbs, parseMode: ParseMode.Html);

            return new[] { photoMessage.MessageId, firstTypeMessage.MessageId, secondTypeMessage.MessageId, photoMessage.MessageId - 1 };
        }


        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id);
            var chatId = callbackQuery.Message.Chat.Id;
            var callBackData = callbackQuery.Data;

            if (callBackData == null) return;

            if (!taskSessions.ContainsKey(chatId)) //чтобы при перезапуске бота старые кнопки не вызывали ошибку
                return;

            if (callBackData.ToLower().Contains("oldbutton"))
                return;

            var task = taskSessions[chatId].Task;
            if (task is ICallBackTask taskCallBack)
                await taskCallBack.HandleCallbackQuery(callbackQuery);
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
            if (admins.Contains(chatId.ToString()))
            {
                keyboard = new ReplyKeyboardMarkup(new[]{
                new[] { new KeyboardButton("📝 Глаголы") },
                new[] { new KeyboardButton("🤖 Чат-бот (Beta)") },
                new[] {new KeyboardButton("💻АДМИНКА💻")},
            })
                {
                    ResizeKeyboard = true
                };
            }


            await _bot.SendMessage(chatId: chatId,
                text: "<b> Навигация осуществляется с помощью меню</b> 👇", replyMarkup: keyboard, parseMode: ParseMode.Html);
        }


        private async Task SendVerbMenu(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("📋 Тип глагола"),
                    new KeyboardButton("🖋️ Перевести"),
                    new KeyboardButton("🛠️ Спряжение")
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
