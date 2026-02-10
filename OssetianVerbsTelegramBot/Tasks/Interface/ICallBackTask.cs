using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot.Tasks.Interface
{
    public interface ICallBackTask
    {
        Task HandleCallbackQuery(CallbackQuery callbackQuery);
    }
}