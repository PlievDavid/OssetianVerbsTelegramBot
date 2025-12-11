using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OssetianVerbsTelegramBot
{
    public class VerbsRepository
    {
        private List<Verb> _verbsData;
        public string Path  { get; set; }
        public VerbsRepository(string path) 
        {
            Path = path;
            LoadVerbs();
        }

        public void LoadVerbs()
        {
            string json = File.ReadAllText(Path);
            _verbsData = JsonSerializer.Deserialize<List<Verb>>(json, options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        public List<Verb> GetAll() => _verbsData;

    }
}
