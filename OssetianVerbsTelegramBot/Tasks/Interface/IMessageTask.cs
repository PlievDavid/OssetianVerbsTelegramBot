using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot.Tasks.Interface
{
    public interface IMessageTask
    {
        Task HandleMessageAnswer(Message message);
    }
}