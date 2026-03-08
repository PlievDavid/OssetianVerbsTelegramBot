using OssetianVerbsTelegramBot.Models;
using OssetianVerbsTelegramBot.Tasks.Interface;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OssetianVerbsTelegramBot.Tasks
{
    public abstract class BaseTask(TelegramBotClient bot) : ITaskState
    {
        protected private TestSession _session;
        protected long chatId;
        protected readonly TelegramBotClient bot = bot;

        public async Task StartTask(Message message)
        {
            chatId = message.Chat.Id;
            _session = new TestSession(chatId, await DbVerbImport.GetSmartRandomVerbs(chatId.ToString()), this);
            BotHandler.SetNewTaskSession(chatId, _session);

            await DbUser.StartStatUpdate(chatId.ToString());
            await SendNextQuestion();
        }


        //этот метод нужно обязательно реализовать в наследнике
        public abstract Task SendNextQuestion();

        public virtual async Task EndTask()
        {
            await bot.SendMessage(chatId, $"Вы закончили тест, количество правильных ответов: {_session.Score}/{_session.CurrentIndex}");
            await DbUser.FillStat(chatId.ToString());
            BotHandler.RemoveTaskSession(_session);
        }

        protected virtual async Task UpdateOldMessageCallback(CallbackQuery callback, bool isRight)
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
            var newKeyboard = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton(text, "oldButton"){Style = isRight ? KeyboardButtonStyle.Success : KeyboardButtonStyle.Danger},
            });
            await bot.EditMessageReplyMarkup(chatId, msg.MessageId, newKeyboard);
        }

        protected virtual async Task HandleCorrectAnswer()
        {
            _session.Score++;
            await DbUser.UpdateUserStat(chatId.ToString(), _session.Verbs[_session.CurrentIndex].Inf, false);
            await bot.SendMessage(chatId, ComplimentGenerator.GetRandomCompliment());
                
        }
        protected abstract  Task HandleIncorrectAnswer();
    }
}