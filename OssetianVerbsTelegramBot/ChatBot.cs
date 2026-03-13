using OssetianVerbsTelegramBot.ApiClients.Yandex;
using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OssetianVerbsTelegramBot
{
    public class ChatBot(TelegramBotClient bot)
    {
        private readonly TelegramBotClient bot = bot;
        private readonly YandexTranslateClient yandexTranslateClient = new YandexTranslateClient(EnvironmentManager.GetYandexGptKey(), EnvironmentManager.GetYandexProjectId());
        private readonly YandexGptClient yandexGptClient = new YandexGptClient(EnvironmentManager.GetYandexGptKey(), EnvironmentManager.GetYandexProjectId());

        private Dictionary<long, ChatSession> chatSessions = new();

        public async Task HandleMessage(Message message)
        {
            var chatId = message.Chat.Id;
            Console.WriteLine($"""User({chatId} - {message.From?.Username ?? "undefind"}): {message.Text}""");

            var loadSmile = await bot.SendSticker(chatId, sticker: "CAACAgUAAxkBAAEVynlphwOBCtgySn0lY4gZRq60cHjnFgACFwsAAnpH2FSrntiSYBUw7ToE");
            var ruMessage = await yandexTranslateClient.TranslateTextAsync(message.Text, "os", "ru");

            chatSessions[chatId].AddHistory($"User: {ruMessage}");

            var response = await yandexGptClient.SendRequestAsync(chatSessions[chatId].ChatHistory);
            Console.WriteLine("GPT: " + response);

            chatSessions[chatId].AddHistory($"GPT: {response}");

            await bot.SendMessage(chatId, $"<b>{await yandexTranslateClient.TranslateTextAsync(response, "ru", "os")}</b>", parseMode: ParseMode.Html);
            await bot.DeleteMessage(chatId, loadSmile.Id);
        }

        public void EnableChatMode(long chatId)
        {
            if (ContainsUser(chatId))
                chatSessions[chatId].IsGptMode = true;
        }

        public void DisableChatMode(long chatId)
        {
            if (ContainsUser(chatId))
                chatSessions[chatId].IsGptMode = false;
        }
        
        public bool ContainsUser(long chatId)
        {
            return chatSessions.ContainsKey(chatId); 
        }

        public void CreateSession(long chatId)
        {
            chatSessions[chatId] = new ChatSession(chatId, false);
        }

        public bool IsChatModeEnabled(long chatId)
        {
            return ContainsUser(chatId) && chatSessions[chatId].IsGptMode;
        }


    }
}
