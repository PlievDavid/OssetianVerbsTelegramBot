using OssetianVerbsTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace OssetianVerbsTelegramBot.Tasks.Interface
{
    public interface ITaskStart
    {
        Task StartTask(Message message);
    }
}