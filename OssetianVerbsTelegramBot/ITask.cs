using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot
{
    public interface ITask
    {
        Task StartTask(Message message);
       
        Task HandleCallbackQuery(CallbackQuery callbackQuery);

        Task SendNextQuestion(long chatId, TestSession session);
    }
}