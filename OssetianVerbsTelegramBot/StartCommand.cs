using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

static class StartCommand
{
    internal static async Task ExecuteAsync(ITelegramBotClient client, Update update)
    {
        if (update != null)
        {
            await client.SendMessage(update.Message.Chat.Id, "ммм", replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] {"1", "2"}));
        }

    }
}