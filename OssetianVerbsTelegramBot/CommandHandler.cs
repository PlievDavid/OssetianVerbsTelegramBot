using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace OssetianVerbsTelegramBot
{
    public class CommandHandler
    {
        public TelegramBotClient _bot;
        public MessageHelper messageHelper;
        public CommandHandler(TelegramBotClient bot, MessageHelper msgh)
        {
            _bot = bot;
            messageHelper = msgh;
        }
        public bool IsCommand(Message msg)
        {
            var command = msg.Text?.Trim();
            if (command == null)
                return false;
            if (command.StartsWith('/'))
                return true;
            return false;
        }

        public async Task HandleCommand(Message CommandMsg)
        {
            var commandSplit = CommandMsg.Text!.Split();
            var chatId = CommandMsg.Chat.Id;
            switch (commandSplit[0])
            {
                case "/start":
                    await DbUser.InitialiseUser(CommandMsg);
                    await messageHelper.SendKeyboardLink(CommandMsg);
                    await messageHelper.SendMainMenu(chatId);
                    return;

                case "/sendto":
                    if (!messageHelper.admins.Contains(chatId)) return;
                    if (commandSplit.Length <= 2) return;
                    if (!Int64.TryParse(commandSplit[1], out long recieverId)) return;
                    if(!DbUser.allUsersId.Contains(recieverId))
                    {
                        await _bot.SendMessage(chatId, "Пользователь не зарегистрирован в боте");
                        return;
                    }

                    var msgToSend = string.Join(" ", commandSplit[2..]);
                    await _bot.SendMessage(recieverId, $"Сообщение от модератора:\n{msgToSend}");
                    await _bot.SendMessage(chatId, "Сообщение отправлено!");
                    return;
            }

        }
    }
}
