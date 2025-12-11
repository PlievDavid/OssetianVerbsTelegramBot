using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

static class StartCommand
{
    internal static async Task ExecuteAsync(ITelegramBotClient client, Update update)
    {
        if (update != null)
        {
            await client.SendMessage(update.Message.Chat.Id, "Приветствую вас в боте для изучения осетинского языка!", 
                replyMarkup : new ReplyKeyboardMarkup(new List<List<KeyboardButton>> { 
                    new List<KeyboardButton>{ new KeyboardButton("Начать учиться"), new KeyboardButton("Теория") },
                    new List<KeyboardButton>{ new KeyboardButton("Статистика") }
                }));

        }
    }

}