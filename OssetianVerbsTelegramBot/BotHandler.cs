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
        private static Dictionary<long, TestSession> _taskSessions = new();
        private static Dictionary<long, ChatSession> _chatSessions = new();
        YandexTranslateClient yandexTranslateClient = new YandexTranslateClient(EnvironmentManager.GetYandexGptKey(), EnvironmentManager.GetYandexProjectId());
        private YandexGptClient yandexGptClient = new YandexGptClient(EnvironmentManager.GetYandexGptKey(), EnvironmentManager.GetYandexProjectId());

        public BotHandler(string token)
        {
            _bot = new TelegramBotClient(token);
        }

        public async Task Start()
        {
            await DbVerbImport.InitializeVerbs();
            await DbSentencesImport.InitializeSentences();
            await DbUser.InitializeAllUsers();
            MessageHelper.Initialize(_bot);
            CommandHandler.Initialize(_bot);
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
                if (!DbUser.IsExistUser(chatId))
                    await DbUser.InitialiseUser(message);

                if (!_chatSessions.ContainsKey(chatId))
                {
                    _chatSessions[message.Chat.Id] = new ChatSession(chatId, false);
                }

                if (MessageHelper.messagesToDelete.ContainsKey(chatId))
                {
                    await MessageHelper.SafeDeleteHelpMessages(chatId);
                }



                if (CommandHandler.IsCommand(message))
                    await CommandHandler.HandleCommand(message);
                else
                {
                    var text = message.Text;
                    switch (text)
                    {
                        case "📝 Глаголы":
                            await MessageHelper.SendVerbMenu(chatId);
                            _chatSessions[chatId].IsGptMode = false;
                            break;
                        case "🤖 Чат-бот (Beta)":
                            _chatSessions[chatId].IsGptMode = true;
                            await _bot.SendMessage(chatId, "<b>Режим чат-бота включен</b> ✅", parseMode: ParseMode.Html);
                            break;



                        case "📋 Тип глагола":
                            ITaskState taskDefineType = new TaskDefineType(_bot);
                            await taskDefineType.StartTask(message);
                            break;

                        case "🖋️ Перевести":
                            ITaskState taskTranslate = new TaskTranslate(_bot);
                            await taskTranslate.StartTask(message);
                            break;

                        case "🛠️ Спряжение":
                            ITaskState taskDeclination = new TaskDeclination(_bot);
                            await taskDeclination.StartTask(message);
                            break;
                        case "⚙️ Статистика":
                            await MessageHelper.SendStatistics(chatId);
                            break;
                        case "🏆 Рейтинг":
                            await MessageHelper.SendRating(chatId,message.Id);
                            break;

                        case "💡 Справка":
                            await MessageHelper.SendHelp(chatId,message.MessageId);
                            break;

                        case "🆘 Обратная связь":
                            await MessageHelper.SendReportHelp(chatId);
                            MessageHelper.needFeedback.Add(chatId);
                            return;

                        case "🔙 Отмена":
                            MessageHelper.needFeedback.Remove(chatId);
                            await MessageHelper.SendMainMenu(chatId);
                            break;

                        case "🔙 В главное меню":
                            await MessageHelper.SendMainMenu(chatId);
                            break;
                        case "👨‍💻 Панель администратора":
                            await MessageHelper.SendAdminMenu(chatId);
                            break;


                        case "📝 Backup Базы данных":
                            if (MessageHelper.admins.Contains(chatId))
                                await DownloadSqliteDatabaseAsync(chatId);
                            break;

                        default:

                            if (MessageHelper.needFeedback.Contains(chatId))
                            {
                                await MessageHelper.SendReportToAllModerators(chatId, message);

                                MessageHelper.needFeedback.Remove(chatId);
                                break;
                            }

                            if (_chatSessions[chatId].IsGptMode)
                            {
                                Console.WriteLine("User(" + chatId + " - " + message.From?.Username ?? "undefind" + "): " + message.Text);

                                var loadSmile = await _bot.SendSticker(chatId, sticker: "CAACAgUAAxkBAAEVynlphwOBCtgySn0lY4gZRq60cHjnFgACFwsAAnpH2FSrntiSYBUw7ToE");

                                var ruMessage = await yandexTranslateClient.TranslateTextAsync(message.Text, "os", "ru");

                                _chatSessions[chatId].AddHistory($"User: {ruMessage}");

                                var response = await yandexGptClient.SendRequestAsync(_chatSessions[chatId].ChatHistory);

                                Console.WriteLine("GPT: " + response);

                                _chatSessions[chatId].AddHistory($"GPT: {response}");

                                await _bot.SendMessage(chatId, $"<b>{await yandexTranslateClient.TranslateTextAsync(response, "ru", "os")}</b>", parseMode: ParseMode.Html);


                                await _bot.DeleteMessage(chatId, loadSmile.Id);
                            }
                            else
                            {
                                if (_taskSessions.ContainsKey(chatId))
                                {
                                    var task = _taskSessions[chatId].Task;
                                    if (_taskSessions[chatId].Sentences.Count != 0 && task is IMessageTask msgTask)
                                        await msgTask.HandleMessageAnswer(message);
                                }
                                else
                                {
                                    await MessageHelper.SendMainMenu(chatId);
                                }
                            }
                            break;
                    }
                }
                MessageHelper.needFeedback.Remove(chatId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id);
            var chatId = callbackQuery.Message.Chat.Id;
            var callBackData = callbackQuery.Data;

            if (callBackData == null) return;
            if (callBackData.ToLower().Contains("ratingid"))
            {
                await MessageHelper.HandleCallbackQuery(callbackQuery);
                return;
            }

            if (!_taskSessions.ContainsKey(chatId))
                return;

            if (callBackData.ToLower().Contains("oldbutton"))
                return;
            var task = _taskSessions[chatId].Task;
            if (task is ICallBackTask taskCallBack)
                await taskCallBack.HandleCallbackQuery(callbackQuery);
        }


        public async Task DownloadSqliteDatabaseAsync(long adminChatId)
        {
            var dbPath = DbVerbImport.dbPath;

            if (!File.Exists(dbPath))
            {
                await _bot.SendMessage(adminChatId, "❌ База данных не найдена");
                return;
            }

            // Делаем копию, так как файл может быть заблокирован SQLite
            var tempDbPath = Path.GetTempFileName();
            try
            {
                File.Copy(dbPath, tempDbPath, overwrite: true);

                await using var stream = File.OpenRead(tempDbPath);
                await _bot.SendDocument(
                    adminChatId,
                    new InputFileStream(stream, $"database_{DateTime.Now:yyyy-MM-dd}.db"),
                    caption: $"🗄 <b>SQLite Database</b>\n📊 Размер: {new FileInfo(dbPath).Length / 1024:N0} KB",
                    parseMode: ParseMode.Html
                );
            }
            finally
            {
                if (File.Exists(tempDbPath))
                    File.Delete(tempDbPath);
            }
        }

        private Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

        public static async void AddNewTaskSession(long chatId, TestSession test)
        {
            _taskSessions[chatId] = test;
        }
        public static async void RemoveTaskSession(TestSession test)
        {
            _taskSessions.Remove(test.UserId);
        }
    }

}
