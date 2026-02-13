using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks.Interface;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.Tasks
{
    public abstract class BaseTask : ITaskState
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
            await DbUser.StartStatUpdate(chatId.ToString());
            await SendNextQuestion(chatId, session);
        }


        //этот метод нужно обязательно реализовать в наследнике
        public abstract Task SendNextQuestion(long chatId, TestSession session);

        public virtual async Task EndTask(long chatId)
        {
            await _bot.SendMessage(chatId, $"Вы закончили тест, количество правильных ответов: {_sessions[chatId].Score}/10");
            await DbUser.FillStat(chatId.ToString());
            _sessions.Remove(chatId);
        }

        protected virtual async Task UpdateOldMessage(CallbackQuery callback, bool isRight)
        {
            var msg = callback.Message;
            if (msg == null) return;
            var text = "";

            if (msg?.ReplyMarkup is InlineKeyboardMarkup keyboard)
            {
                foreach (var row in keyboard.InlineKeyboard)
                {
                    foreach (var button in row)
                        if (button.CallbackData == callback.Data)
                        {
                            text = button.Text;
                            break;
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