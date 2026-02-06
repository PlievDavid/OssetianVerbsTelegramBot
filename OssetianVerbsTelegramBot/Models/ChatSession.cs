using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models
{
    internal class ChatSession
    {
        public long UserId { get; set; }
        public string ChatHistory { get; set; } = string.Empty;
        public bool IsGptMode { get; set; }
        public ChatSession(long userId, bool isGptMode)
        {
            UserId = userId;
            IsGptMode = isGptMode;
        }
    }
}
