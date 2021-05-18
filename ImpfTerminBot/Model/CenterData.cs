using Newtonsoft.Json;

namespace ImpfTerminBot.Model
{
    public class CenterData
    {
        [JsonProperty("Zentrumsname")]
        public string CenterName { get; set; }

        [JsonProperty("PLZ")]
        public string Postcode { get; set; }

        [JsonProperty("Ort")]
        public string City { get; set; }

        [JsonProperty("Bundesland")]
        public string Country { get; set; }

        [JsonProperty("URL")]
        public string Url { get; set; }

        [JsonProperty("Adresse")]
        public string Adress { get; set; }
    }
}
