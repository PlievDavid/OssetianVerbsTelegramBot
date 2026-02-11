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
        private List<string> chatHistory;
        public string ChatHistory
        {
            get => string.Join("\n", chatHistory);
        }
            
        public bool IsGptMode { get; set; }
        public ChatSession(long userId, bool isGptMode)
        {
            chatHistory = new List<string>();
            UserId = userId;
            IsGptMode = isGptMode;
        }
        public void AddHistory(string history)
        {
            if(chatHistory.Count >= 6)
                chatHistory.RemoveRange(0,2);

            chatHistory.Add(history);
        }
    }
}
