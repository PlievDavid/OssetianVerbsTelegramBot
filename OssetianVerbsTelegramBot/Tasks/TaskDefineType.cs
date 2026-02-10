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
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Тип 1 -тон (переходный) ", "answer_1"),

                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Тип 2 -тӕн (непереходный)", "answer_2")
                }

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
            var callBackData = callbackQuery.Data;
            if (callBackData.StartsWith("answer_"))
            {
                var chatId = callbackQuery.Message.Chat.Id;
                int answer = int.Parse(callbackQuery.Data.Split('_')[1]);
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
                    await _bot.SendMessage(chatId, $"Тест завершён!\nРезультат: {session.Score}/10");
                    _sessions.Remove(chatId);
                }
            }
        }

        private async Task UpdateOldMessage(CallbackQuery callback, bool isRight)
        {
            var msg = callback.Message;
            if (msg == null) return;
            var text = "";

            if (msg?.ReplyMarkup is InlineKeyboardMarkup keyboard)
            {
                foreach (var row in keyboard.InlineKeyboard)
                {
                    foreach (var button in row)
                    {
                        if (button.CallbackData == callback.Data)
                            text = button.Text;
                    }
                }
            }
            else return;
            text += isRight ? "✅" : "❌";
            var newKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData(text, "oldButton")
            });
            await _bot.EditMessageReplyMarkup(msg.Chat.Id, msg.MessageId, newKeyboard);
        }
    }
}
