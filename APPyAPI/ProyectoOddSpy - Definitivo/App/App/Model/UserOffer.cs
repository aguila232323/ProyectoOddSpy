namespace App.Model
{
    public class UserOffer
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OfferId { get; set; }
        public decimal LiberatedAmount { get; set; }
        public OfferStatus Status { get; set; }
        public Offers Offer { get; set; }
    }

    public enum OfferStatus
    {
        Activated = 0,
        Completed = 1,
        CreditsReleased = 2
    }
}
