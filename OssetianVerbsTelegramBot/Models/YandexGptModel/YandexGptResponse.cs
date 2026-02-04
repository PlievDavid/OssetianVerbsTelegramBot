using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.Models.YandexGptModel
{
    internal class YandexGptResponse
    {
        [JsonPropertyName("output")]
        public string Output { get; set; } // ← ГЛАВНОЕ ПОЛЕ!

        [JsonPropertyName("model_used")]
        public string ModelUsed { get; set; }

        [JsonPropertyName("tokens_used")]
        public int TokensUsed { get; set; }
    }
}
