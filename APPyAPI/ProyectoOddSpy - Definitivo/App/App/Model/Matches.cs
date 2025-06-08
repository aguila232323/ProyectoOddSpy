

namespace App.Model
{
    public class Matches
    {
        public string HomeTeam { get; set; }
        public string HomeTeamImg { get; set; }
        public string AwayTeam { get; set; }
        public string AwayTeamImg { get; set; }
        public string Time { get; set; }
        public string Casino1 { get; set; }
        public string Odds1 { get; set; }
        public string CasinoX { get; set; }
        public string OddsX { get; set; }
        public string Casino2 { get; set; }
        public string Odds2 { get; set; }
        public string Type { get; set; }
        public string BenefitPecentaje { get; set; }


        public bool EsIgualA(Matches otro)
        {
            return Odds1 == otro.Odds1 &&
                   OddsX == otro.OddsX &&
                   Odds2 == otro.Odds2 &&
                   HomeTeam == otro.HomeTeam &&
                   HomeTeamImg == otro.HomeTeamImg &&
                   AwayTeam == otro.AwayTeam && 
                   AwayTeamImg == otro.AwayTeamImg &&
                   Casino1 == otro.Casino1 &&
                   CasinoX == otro.CasinoX &&
                   Casino2 == otro.Casino2 &&
                   BenefitPecentaje == otro.BenefitPecentaje &&
                   Time == otro.Time;
        }


    }
}
