namespace API.Models.Entities
{
    public class Offer
    {
        public Guid Id { get; set; }
        public string Casino { get; set; }
        public string BannerImg { get; set; }
        public string Descryption { get; set; }
        public string Title { get; set; }
        public int MaxFreeAmount { get; set; }
        public string Type { get; set; }

        public ICollection<UserOffer> UserOffers { get; set; } = new List<UserOffer>();

    }
}
