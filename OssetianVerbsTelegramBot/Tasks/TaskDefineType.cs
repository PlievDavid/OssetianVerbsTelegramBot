using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.Tasks
{
    internal class TaskDefineType : BaseTask, ICallBackTask
    {
        public TaskDefineType(TelegramBotClient bot) : base(bot) { }

        public override async Task SendNextQuestion()
        {
            var verb = _session.Verbs[_session.CurrentIndex];
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Тип 1 -тон (переходный) ", "defineTypeAns:1") },
                new[] { InlineKeyboardButton.WithCallbackData("Тип 2 -тӕн (непереходный)", "defineTypeAns:2") }
            });
            await bot.SendMessage(
                chatId,
                $"№{_session.CurrentIndex + 1}/10\n\nОпределите тип глагола: <b>{verb.Inf}</b>",
                replyMarkup: keyboard,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var callbackData = callbackQuery.Data;
            if (!callbackData.StartsWith("defineTypeAns"))
                return;

            int answer = int.Parse(callbackData.Split(':')[1]);
            var verb = _session.Verbs[_session.CurrentIndex];

            if (answer == verb.Type)
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
            var type = _session.Verbs[_session.CurrentIndex].Type == 1 ? "тип 1 (переходный)" : "тип 2 (непереходный)";
            await bot.SendMessage(chatId, "Неверно! Это " + type);
        }
    }
}
