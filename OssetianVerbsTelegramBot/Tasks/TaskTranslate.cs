using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks.Interface;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.Tasks
{
    internal class TaskTranslate : BaseTask, ICallBackTask
    {
        public TaskTranslate(TelegramBotClient bot, Dictionary<long, TestSession> sessions) : base(bot, sessions) { }

        public override async Task SendNextQuestion(long chatId, TestSession session)
        {
            
            if (session.CurrentIndex > session.Verbs.Count-1)
            {
                await _bot.SendMessage(chatId, $"Вы закончили тест, количество правильных ответов: {session.Score}/10");
                await DbUser.FillStat(chatId.ToString());
                return;
            }

            var verb = session.Verbs[session.CurrentIndex];
            var wrongVerb = DbVerbImport.GetRandomVerb();

            var twoVerbs = new List<Verb> { verb, wrongVerb };
            int randomNum = Random.Shared.Next(1, 25) % 2;
            InlineKeyboardMarkup answers =
                new InlineKeyboardMarkup(
                    new InlineKeyboardButton(twoVerbs[randomNum].Trans, twoVerbs[randomNum].Trans), 
                    new InlineKeyboardButton(twoVerbs[1 - randomNum].Trans, twoVerbs[1 - randomNum].Trans));


            await _bot.SendMessage(chatId, $"№{session.CurrentIndex + 1}/10 \n\nПереведите слово на русский язык: <b>{verb.Inf}</b>", replyMarkup: answers, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public  async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;

            var session = _sessions[chatId];

            if (callbackQuery.Data == session.Verbs[session.CurrentIndex].Trans)
            {
                session.Score++;
                await DbUser.UpdateUserStat(chatId.ToString(), session.Verbs[session.CurrentIndex].Inf, false);
                await _bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());
            }
            else
            {
                await DbUser.UpdateUserStat(chatId.ToString(), session.Verbs[session.CurrentIndex].Inf, true);
                await _bot.SendMessage(chatId, "Неверно! Правильно: " + session.Verbs[session.CurrentIndex].Trans);
            }

            session.CurrentIndex++;
            await SendNextQuestion(chatId, session);
        }
    }
}