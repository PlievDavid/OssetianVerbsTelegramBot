using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot.ApiClients.Yandex
{
    internal class YandexTranslateClient
    {

        readonly private string _apiKey;
        readonly private string _apiProjectId;

        public YandexTranslateClient(string apiKey, string apiProjectId)
        {
            _apiKey = apiKey;
            _apiProjectId = apiProjectId;
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            RestClientOptions restClientOptions = new RestClientOptions("https://translate.api.cloud.yandex.net/");
            RestClient restClient = new RestClient(restClientOptions);
            RestRequest restRequest = new RestRequest("translate/v2/translate", Method.Post);

            restRequest.AddHeader("Authorization", $"Api-Key {_apiKey}");

            restRequest.AddBody(new
            {
                source_language_code = sourceLanguage,
                target_language_code = targetLanguage,
                texts = text,
                folder_id = _apiProjectId
            });
            
            var response = await restClient.ExecuteAsync(restRequest);
            dynamic data = JObject.Parse(response.Content);
            return data.translations[0].text;
        }
    }
}
