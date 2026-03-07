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
        public TaskTranslate(TelegramBotClient bot) : base(bot) { }

        public override async Task SendNextQuestion()
        {
            var verb = _session.Verbs[_session.CurrentIndex];
            var wrongVerb = DbVerbImport.GetRandomVerb();

            var twoVerbs = new List<Verb> { verb, wrongVerb };
            int randomNum = Random.Shared.Next(1, 25) % 2;
            InlineKeyboardMarkup answers =
                new InlineKeyboardMarkup(
                    new InlineKeyboardButton(twoVerbs[randomNum].Trans, "translateAns:"+twoVerbs[randomNum].Trans), 
                    new InlineKeyboardButton(twoVerbs[1 - randomNum].Trans, "translateAns:"+twoVerbs[1 - randomNum].Trans));


            await _bot.SendMessage(chatId, $"№{_session.CurrentIndex + 1}/10 \n\nПереведите слово на русский язык: <b>{verb.Inf}</b>", replyMarkup: answers, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        public  async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var callbackData = callbackQuery.Data;
            if (!callbackData.StartsWith("translateAns"))
                return;

            var answer = callbackData.Split(':')[1];

            if (answer == _session.Verbs[_session.CurrentIndex].Trans)
            {
                await UpdateOldMessageCallback(callbackQuery, true);
                await HandleCorrectAnswer();
            }
            else
            {
                await UpdateOldMessageCallback(callbackQuery, false);
                await HandleIncorrectAnswer();
            }

            _session.CurrentIndex++;

            if (_session.CurrentIndex < _session.Verbs.Count)
                await SendNextQuestion();
            else
                await EndTask();
        }

        protected override async Task HandleIncorrectAnswer()
        {
            await DbUser.UpdateUserStat(chatId.ToString(), _session.Verbs[_session.CurrentIndex].Inf, true);
            await _bot.SendMessage(chatId, "Неверно! Правильно: " + _session.Verbs[_session.CurrentIndex].Trans);
        }
    }
}