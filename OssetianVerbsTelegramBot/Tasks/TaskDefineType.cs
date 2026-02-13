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
        public TaskDefineType(TelegramBotClient bot, Dictionary<long, TestSession> sessions) : base(bot, sessions) { }

        public override async Task SendNextQuestion(long chatId, TestSession session)
        {
            var verb = session.Verbs[session.CurrentIndex];
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Тип 1 -тон (переходный) ", "defineTypeAns:1") },
                new[] { InlineKeyboardButton.WithCallbackData("Тип 2 -тӕн (непереходный)", "defineTypeAns:2") }
            });
            await _bot.SendMessage(
                chatId,
                $"№{session.CurrentIndex + 1}/10\n\nОпределите тип глагола: <b>{verb.Inf}</b>",
                replyMarkup: keyboard,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var callbackData = callbackQuery.Data;
            if (!callbackData.StartsWith("defineTypeAns"))
                return;

            var chatId = callbackQuery.Message.Chat.Id;
            int answer = int.Parse(callbackData.Split(':')[1]);
            var session = _sessions[callbackQuery.Message.Chat.Id];
            var verb = session.Verbs[session.CurrentIndex];

            if (answer == verb.Type)
            {
                session.Score++;
                await DbUser.UpdateUserStat(chatId.ToString(), verb.Inf, false);

                await UpdateOldMessage(callbackQuery, true);
                await _bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());

            }
            else
            {
                await DbUser.UpdateUserStat(chatId.ToString(), verb.Inf, true);
                await UpdateOldMessage(callbackQuery, false);
                await _bot.SendMessage(chatId, $"Неправильный ответ!❌");
            }

            session.CurrentIndex++;
            if (session.CurrentIndex < session.Verbs.Count)
                await SendNextQuestion(chatId, session);

            else
            {
                await EndTask(chatId);
            }
        }

    }
}
