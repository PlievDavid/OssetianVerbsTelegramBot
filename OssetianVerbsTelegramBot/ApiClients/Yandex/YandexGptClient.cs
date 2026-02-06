using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using OssetianVerbsTelegramBot.Models.YandexGptModel;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.ApiClients.Yandex
{
    public class YandexGptClient
    {
        readonly private string _apiKey;
        readonly private string _apiProjectId;

        public YandexGptClient(string apiKey, string apiProjectId)
        {
            _apiKey = apiKey;
            _apiProjectId = apiProjectId;
        }

        public async Task<string> SendRequestAsync(string text)
        {
            RestClientOptions restClientOptions = new RestClientOptions("https://ai.api.cloud.yandex.net");

            RestClient restClient = new RestClient(restClientOptions);

            RestRequest restRequest = new RestRequest("/v1/responses", Method.Post);

            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Authorization", $"Api-Key {_apiKey}");
            restRequest.AddHeader("OpenAI-Project", _apiProjectId);

            restRequest.AddJsonBody(new YandexGptRequest()
            {
                Model = "gpt://b1g8l0frn3gd9j5d3db2/gemma-3-27b-it/latest",
                Instructions = "Отвечай на все! Никогда не оставляй сообщение не отвеченным. Форматировать текст не надо. Коротко. Если не знаешь, говори, что не знаешь.",
                Input = text,
                Temperature = 0.3,
                MaxOutputTokens = 500
            });

            var response = await restClient.ExecuteAsync(restRequest);
            dynamic data = JObject.Parse(response.Content);
            return data.output[0].content[0].text;
        }
    }
}
