using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks.Interface;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot.Tasks
{
    public abstract class BaseTask : ITaskStart
    {
        protected readonly Dictionary<long, TestSession> _sessions;
        protected readonly TelegramBotClient _bot;

        protected BaseTask(TelegramBotClient bot, Dictionary<long, TestSession> sessions)
        {
            _sessions = sessions;
            _bot = bot;
        }

        public async Task StartTask(Message message)
        {
            var chatId = message.Chat.Id;
            _sessions[chatId] = new TestSession(chatId, await DbVerbImport.GetSmartRandomVerbs(chatId.ToString()), this);

            var session = _sessions[chatId];

            await SendNextQuestion(chatId, session);
        }


        //этот метод нужно обязательно реализовать в наследнике
        public abstract Task SendNextQuestion(long chatId, TestSession session);
    }
}