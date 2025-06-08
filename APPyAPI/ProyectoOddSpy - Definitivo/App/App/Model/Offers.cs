using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App.Model
{
    public class Offers
    {

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("casino")]
        public string Casino { get; set; }

        [JsonPropertyName("bannerImg")]
        public string BannerImg { get; set; }

        [JsonPropertyName("descryption")]
        public string Descryption { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("maxFreeAmount")]
        public int MaxFreeAmount { get; set; }

        [JsonPropertyName("liberatedAmount")]
        public decimal LiberatedAmount { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("userOffers")]
        public List<object> UserOffers { get; set; }


        public bool IsRegistered { get; set; } = false;

    }
}