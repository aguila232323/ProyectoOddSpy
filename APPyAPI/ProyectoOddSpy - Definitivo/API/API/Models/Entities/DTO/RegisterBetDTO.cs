namespace API.Models.Entities.DTO
{
    public class RegisterBetDTO
    {
        public Guid UserOfferId { get; set; }
        public decimal Amount { get; set; }
        public string HomeTeam { get; set; }
        public string HomeTeamImg { get; set; }
        public string AwayTeam { get; set; }
        public string AwayTeamImg { get; set; }
        public string Casino1 { get; set; }
        public decimal AmountCasino1 { get; set; }
        public string Odds1 { get; set; }
        public string CasinoX { get; set; }
        public decimal AmountCasinoX { get; set; }
        public string OddsX { get; set; }
        public string Casino2 { get; set; }
        public decimal AmountCasino2 { get; set; }
        public string Odds2 { get; set; }
    }
}
