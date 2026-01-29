using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.DefineTypeTask
{
    internal class TaskDefineType:ITaskHelper
    {
        readonly Dictionary<long, TestSession> _sessions;
        readonly TelegramBotClient _bot;

        public TaskDefineType(TelegramBotClient bot, Dictionary<long, TestSession> sessions)
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
            var verb = session.Verbs[session.CurrentIndex];
            Console.WriteLine(session.CurrentIndex);
            foreach (var item in session.Verbs)
            {
                Console.Write(item.Inf + " ");
            }
            Console.WriteLine(session.CurrentIndex);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Тип 1", "answer_1"),
                    
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Тип 2", "answer_2")
                }
                
            });
            Console.WriteLine($"{verb.Inf} {verb.Type}");
            await _bot.SendMessage(
                chatId,
                $"№{session.CurrentIndex + 1}/10\n\nОпределите тип глагола (переходный или непереходный): <b>{verb.Inf}</b>",
                replyMarkup: keyboard,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            int answer = int.Parse(callbackQuery.Data.Split('_')[1]);
            var session = _sessions[callbackQuery.Message.Chat.Id];
            var verb = session.Verbs[session.CurrentIndex];

            if (answer == verb.Type)
            {
                session.Score++;
                await DbUser.UpdateUserStat(chatId.ToString(), verb.Inf, false);
                await _bot.SendMessage(chatId, $"Правильный ответ!✅");

            }
            else
            {
                await DbUser.UpdateUserStat(chatId.ToString(), verb.Inf, true);
                await _bot.SendMessage(chatId, $"Неправильный ответ!❌");
            }

            session.CurrentIndex++;

            if (session.CurrentIndex < session.Verbs.Count)
                await SendNextQuestion(chatId,session);

            else
            {
                await _bot.SendMessage(chatId, $"Тест завершён!\nРезультат: {session.Score}/10");
                _sessions.Remove(chatId);
            }
        }
    }
}
