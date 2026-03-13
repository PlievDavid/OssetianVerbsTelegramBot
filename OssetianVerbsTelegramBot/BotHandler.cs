using Microsoft.Extensions.DependencyInjection;
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
    public class BotHandler(TelegramBotClient bot, MessageHelper messageHelper, CommandHandler commandHandler, ChatBot chatBot)
    {

        readonly TelegramBotClient bot = bot;
        readonly MessageHelper messageHelper = messageHelper;
        readonly CommandHandler commandHandler = commandHandler;
        readonly ChatBot chatBot = chatBot;

        public static Dictionary<long, TestSession> _taskSessions = new();

        public async Task Start()
        {
            await DbVerbImport.InitializeVerbs();
            await DbSentencesImport.InitializeSentences();
            await DbUser.InitializeAllUsers();

            bot.StartReceiving(UpdateHandler, ErrorHandler);
            ScoreResetService.Start();
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
                await HandleCallbackQuery(update.CallbackQuery!);
            }
        }

        private async Task HandleMessage(Message message)
        {
            try
            {
                var chatId = message.Chat.Id;
                if (!DbUser.IsExistUser(chatId))
                    await DbUser.InitialiseUser(message);

                if (!chatBot.ContainsUser(chatId))
                    chatBot.CreateSession(chatId);

                if (messageHelper.messagesToDelete.ContainsKey(chatId))
                    await messageHelper.SafeDeleteHelpMessages(chatId);


                if (commandHandler.IsCommand(message))
                    await commandHandler.HandleCommand(message);
                else
                {
                    var text = message.Text;
                    switch (text)
                    {
                        case "📝 Глаголы":
                            chatBot.DisableChatMode(chatId);
                            await messageHelper.SendVerbMenu(chatId);
                            break;
                        case "🤖 Чат-бот (Beta)":
                            chatBot.EnableChatMode(chatId);
                            await bot.SendMessage(chatId, "<b>Режим чат-бота включен</b> ✅", parseMode: ParseMode.Html);
                            break;



                        case "📋 Тип глагола":
                            ITaskState taskDefineType = new TaskDefineType(bot);
                            await taskDefineType.StartTask(message);
                            break;

                        case "🖋️ Перевести":
                            ITaskState taskTranslate = new TaskTranslate(bot);
                            await taskTranslate.StartTask(message);
                            break;

                        case "🛠️ Спряжение":
                            ITaskState taskDeclination = new TaskDeclination(bot);
                            await taskDeclination.StartTask(message);
                            break;
                        case "⚙️ Статистика":
                            await messageHelper.SendStatistics(chatId);
                            break;
                        case "🏆 Рейтинг":
                            await messageHelper.SendRating(chatId,message.Id);
                            break;

                        case "💡 Справка":
                            await messageHelper.SendHelp(chatId,message.MessageId);
                            break;

                        case "🆘 Обратная связь":
                            await messageHelper.SendReportHelp(chatId);
                            messageHelper.needFeedback.Add(chatId);
                            return;

                        case "🔙 Отмена":
                            messageHelper.needFeedback.Remove(chatId);
                            await messageHelper.SendMainMenu(chatId);
                            break;

                        case "🔙 В главное меню":
                            await messageHelper.SendMainMenu(chatId);
                            break;
                        case "👨‍💻 Панель администратора":
                            await messageHelper.SendAdminMenu(chatId);
                            break;


                        case "📝 Backup Базы данных":
                            if (messageHelper.admins.Contains(chatId))
                                await DownloadSqliteDatabaseAsync(chatId);
                            break;

                        default:

                            if (messageHelper.needFeedback.Contains(chatId))
                            {
                                await messageHelper.SendReportToAllModerators(chatId, message);
                                messageHelper.needFeedback.Remove(chatId);
                                break;
                            }

                            if (chatBot.IsChatModeEnabled(chatId))
                                await chatBot.HandleMessage(message);
                            else
                            {
                                if (_taskSessions.ContainsKey(chatId))
                                {
                                    var task = _taskSessions[chatId].Task;
                                    if (_taskSessions[chatId].Sentences.Count != 0 && task is IMessageTask msgTask)
                                        await msgTask.HandleMessageAnswer(message);
                                }
                                else
                                    await messageHelper.SendMainMenu(chatId);
                            }
                            break;
                    }
                }
                messageHelper.needFeedback.Remove(chatId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id);
            var callBackData = callbackQuery.Data;

            if (callBackData == null) return;

            var chatId = callbackQuery!.Message!.Chat.Id;
            if (callBackData.ToLower().Contains("ratingid"))
            {
                await messageHelper.HandleCallbackQuery(callbackQuery);
                return;
            }

            if (callBackData.Contains("oldbutton"))
                return;

            if (!_taskSessions.ContainsKey(chatId))
                return;
            var task = _taskSessions[chatId].Task;
            if (task is ICallBackTask taskCallBack)
                await taskCallBack.HandleCallbackQuery(callbackQuery);
        }


        private async Task DownloadSqliteDatabaseAsync(long adminChatId)
        {
            var dbPath = DbVerbImport.dbPath;

            if (!File.Exists(dbPath))
            {
                await bot.SendMessage(adminChatId, "❌ База данных не найдена");
                return;
            }

            // Делаем копию, так как файл может быть заблокирован SQLite
            var tempDbPath = Path.GetTempFileName();
            try
            {
                File.Copy(dbPath, tempDbPath, overwrite: true);

                await using var stream = File.OpenRead(tempDbPath);
                await bot.SendDocument(
                    adminChatId,
                    new InputFileStream(stream, $"VerbsDb_{DateTime.Now:yyyy-MM-dd}.db"),
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

        public static async void SetNewTaskSession(long chatId, TestSession test)
        {
            _taskSessions[chatId] = test;
        }
        public static async void RemoveTaskSession(TestSession test)
        {
            _taskSessions.Remove(test.UserId);
        }
    }

}
