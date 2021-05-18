using ImpfTerminBot.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ImpfTerminBot
{
    public class CountryDataReader
    {
        public string Url { get; set; } = "https://www.impfterminservice.de/assets/static/impfzentren.json";

        public List<CountryData> ReadFromUrl()
        {
            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString(Url);
                return ParseJson(json);
            }
        }

        private List<CountryData> ParseJson(string json)
        {
            var jobject = JObject.Parse(json);
            var list = new List<CountryData>();
            foreach (var item in jobject.Children())
            {
                var country = item.Path;
                var centers = item.First.ToString();

                var centerData = JsonConvert.DeserializeObject<List<CenterData>>(centers);
                var countryData = new CountryData()
                {
                    Country = country,
                    Centers = centerData
                };

                list.Add(countryData);
            }

            list = RemoveCentersWithoutPostcode(list);
            return list;
        }

        private List<CountryData> RemoveCentersWithoutPostcode(List<CountryData> countryData)
        {
            foreach (var item in countryData)
            {
                item.Centers.RemoveAll(x => x.Postcode == "");
            }
            countryData.RemoveAll(x => x.Centers.Count == 0);
            return countryData;
        }

        public List<CountryData> ReadFromFile(string fileName)
        {
            if(!File.Exists(fileName))
            {
                throw new FileNotFoundException($"Datei {fileName} konnte nicht gefunden werden.");
            }

            var json = File.ReadAllText(fileName);
            return ParseJson(json);
        }
    }
}
