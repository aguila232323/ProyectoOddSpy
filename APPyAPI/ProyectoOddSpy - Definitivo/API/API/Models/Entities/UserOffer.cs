namespace API.Models.Entities
{
    public class UserOffer
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        public OfferStatus Status { get; set; }
        public decimal LiberatedAmount { get; set; }


        public ICollection<Bet> Bets { get; set; }
    }
    public enum OfferStatus
    {
        NotClaimed,
        Activated,
        CreditsReleased,
        Completed
    }
}
