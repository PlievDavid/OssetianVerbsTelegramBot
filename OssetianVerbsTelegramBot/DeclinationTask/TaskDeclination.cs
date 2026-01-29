using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.DeclinationTask
{
    internal class TaskDeclination : ITaskHelper
    {
        readonly Dictionary<long, TestSession> _sessions;
        readonly TelegramBotClient _bot;

        public TaskDeclination(TelegramBotClient bot, Dictionary<long, TestSession> sessions)
        {
            _sessions = sessions;
            _bot = bot;
        }
        public async Task StartTask(Message message)
        {
            var chatId = message.Chat.Id;

            var session = _sessions[chatId];

            await SendNextQuestion(chatId, session);
        }
        public async Task SendNextQuestion(long chatId, TestSession session)
        {

            if (session.CurrentIndexDeclinationTask > session.Verbs.Count - 1)
            {
                await _bot.SendMessage(chatId, $"Вы закончили тест, количество правильных ответов: {session.ScoreDeclinationTask}/10");
                return;
            }

            var sentence = await DbSentencesImport.GetRandomSentenceByVerbInf(session.Verbs[session.CurrentIndexDeclinationTask].Inf);
            _sessions[chatId].Sentences.Add(sentence);
            await _bot.SendMessage(chatId, $"№{session.CurrentIndexDeclinationTask + 1}/10\n\nПереведите предложение: <b>{sentence.Russian}</b>", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            
        }
        public async Task HandleMessageAnswer(Message message)
        {
            var chatId = message.Chat.Id;

            var session = _sessions[chatId];
            if (message.Text.Trim() == session.Sentences[session.CurrentIndexDeclinationTask].Ossetian)
            {
                session.ScoreDeclinationTask++;
                await DbUser.UpdateUserStat(chatId.ToString(), session.Sentences[session.CurrentIndexDeclinationTask].VerbInf, false);
                await _bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());
            }
            else
            {
                await DbUser.UpdateUserStat(chatId.ToString(), session.Sentences[session.CurrentIndexDeclinationTask].VerbInf, true);
                await _bot.SendMessage(chatId, "Неверно! Правильно: " + session.Sentences[session.CurrentIndexDeclinationTask].Ossetian);
            }

            session.CurrentIndexDeclinationTask++;
            await SendNextQuestion(chatId, session);
        }
    }
}
