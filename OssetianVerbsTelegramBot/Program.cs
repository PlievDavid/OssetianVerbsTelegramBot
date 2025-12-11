using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using DotNetEnv;
using Telegram.Bot.Types.ReplyMarkups;

internal class Program
{
    static async Task Main(string[] args)
    {
        string token = GetBotToken();

        var bot = new TelegramBotClient(token);

        Console.WriteLine("Бот запущен!");

        bot.StartReceiving(UpdateHandler, ErrorHandler);

        await Task.Delay(-1);
    }

    private static async Task ErrorHandler(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
    {
       
    }

    private static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
    {
        var message = update.Message;
        Console.WriteLine($"Получено обновление: {update.Id}, Type: {update.Type}");
        if (message != null)
        {
            if (message.Text.StartsWith("/start"))
            {
                await StartCommand.ExecuteAsync(client, update);
            }
            else if (message.Text.StartsWith("Начать учиться", StringComparison.OrdinalIgnoreCase))
            {
                await LearnCommand.ExecuteAsync(client, update);
            }
            else if (message.Text.StartsWith("Статистика", StringComparison.OrdinalIgnoreCase))
            {
                await ShowStatisticsCommand.ExecuteAsync(client, update);
            }
        }
    }

    public static string GetBotToken()
    {
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        if (token != null)
        {
            return token;
        }

        if (File.Exists(".env"))
        {
            Env.Load();

            token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            if (token != null)
            {
                return token;
            }
        }
        throw new Exception("Токен бота не найден!");
    }
}