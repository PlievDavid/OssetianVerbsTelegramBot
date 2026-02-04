using Microsoft.EntityFrameworkCore.Metadata;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
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
            RestClientOptions restClientOptions = new RestClientOptions("https://ai.api.cloud.yandex.net") { Timeout = new TimeSpan(3000)};

            RestClient restClient = new RestClient(restClientOptions);

            RestRequest restRequest = new RestRequest();

            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Authorization", $"Api-Key {_apiKey}");
            restRequest.AddHeader("OpenAI-Project", _apiProjectId);

            restRequest.AddJsonBody(new
            {
                model = "gpt://b1g8l0frn3gd9j5d3db2/gpt-oss-120b/latest",
                instructions = "Пиши по русски",
                input = "Привет, как дела?",
                temperature = 0.3,
                max_output_tokens = 500
            });

            var response = restClient.ExecuteAsync(restRequest);

            return response.Result.Content;
        }
    }
}
