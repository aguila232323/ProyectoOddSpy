namespace API.Models.Entities.DTO
{
    public class RegisterUserToOfferDTO
    {
        public Guid UserId { get; set; }
        public Guid OfferId { get; set; }
    }
}
