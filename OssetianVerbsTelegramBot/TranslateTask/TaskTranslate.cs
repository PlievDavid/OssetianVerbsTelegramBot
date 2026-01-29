using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OssetianVerbsTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.TranslateTask
{
    internal class TaskTranslate: ITaskHelper
    {
        readonly Dictionary<long, TestSession> _sessions;
        readonly TelegramBotClient _bot;
        Random rnd = new Random();

        public TaskTranslate(TelegramBotClient bot, Dictionary<long, TestSession> sessions)
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
            
            if (session.CurrentIndexTranslateTask > session.Verbs.Count-1)
            {
                await _bot.SendMessage(chatId, $"Вы закончили тест, количество правильных ответов: {session.ScoreTranslateTask}/10");
                return;
            }

            var verb = session.Verbs[session.CurrentIndexTranslateTask];
            var wrongVerb = await DbVerbImport.GetRandomVerb();

            var twoVerbs = new List<Verb> { verb, wrongVerb };
            int randomNum = rnd.Next(1, 25) % 2;
            InlineKeyboardMarkup answers =
                new InlineKeyboardMarkup(
                    new InlineKeyboardButton(twoVerbs[randomNum].Trans, twoVerbs[randomNum].Trans), 
                    new InlineKeyboardButton(twoVerbs[1 - randomNum].Trans, twoVerbs[1 - randomNum].Trans));


            await _bot.SendMessage(chatId, $"№{session.CurrentIndexTranslateTask + 1}/10 \n\nПереведите слово на русский язык: <b>{verb.Inf}</b>", replyMarkup: answers, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;

            var session = _sessions[chatId];

            if (callbackQuery.Data == session.Verbs[session.CurrentIndexTranslateTask].Trans)
            {
                session.ScoreTranslateTask++;
                await DbUser.UpdateUserStat(chatId.ToString(), session.Verbs[session.CurrentIndexTranslateTask].Inf, false);
                await _bot.SendMessage(chatId, "Молодец! Правильно!");
            }
            else
            {
                await DbUser.UpdateUserStat(chatId.ToString(), session.Verbs[session.CurrentIndexTranslateTask].Inf, true);
                await _bot.SendMessage(chatId, "Неверно! Правильно: " + session.Verbs[session.CurrentIndexTranslateTask].Trans);
            }

            session.CurrentIndexTranslateTask++;
            await SendNextQuestion(chatId, session);
        }
    }
}