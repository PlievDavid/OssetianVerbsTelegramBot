using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

static class StartCommand
{
    internal static async Task ExecuteAsync(ITelegramBotClient client, Update update)
    {
        var inlineKeyboard = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("Учить", "1")],
            [InlineKeyboardButton.WithCallbackData("Статистика", "2")]
            ]);
        if (update != null)
        {
            await client.SendMessage(update.Message.Chat.Id, "Приветствую вас в боте для изучения осетинского языка!", replyMarkup: inlineKeyboard);

            if (update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(client, update.CallbackQuery);
            }
        }
    }

    public static async Task HandleCallbackQueryAsync(ITelegramBotClient client, CallbackQuery? callbackQuery)
    {
        await client.AnswerCallbackQuery(callbackQuery.Id, "Привет");
        Console.WriteLine(callbackQuery.Data);
        if (callbackQuery.Data == "1")
        {
            await client.SendMessage(callbackQuery.Message.Chat.Id, "1");
        }

        if (callbackQuery.Data == "2")
        {
            await client.SendMessage(callbackQuery.Message.Chat.Id, "2");
        }
    }
}