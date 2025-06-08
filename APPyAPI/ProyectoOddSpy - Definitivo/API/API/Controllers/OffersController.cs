using API.Data;
using API.Models.Entities;
using API.Models.Entities.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public OffersController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Offers
        [HttpGet]
        public async Task<IActionResult> GetOffers()
        {
            var offers = await _dbContext.Offers.ToListAsync();
            return Ok(offers);
        }

        // POST: api/Offers/register
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUserToOffer(RegisterUserToOfferDTO dto)
        {
            var user = await _dbContext.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("Usuario no encontrado");

            var offer = await _dbContext.Offers.FindAsync(dto.OfferId);
            if (offer == null) return NotFound("Oferta no encontrada");

            var existing = await _dbContext.UserOffers
                .FirstOrDefaultAsync(uo => uo.UserId == dto.UserId && uo.OfferId == dto.OfferId);

            if (existing != null)
                return BadRequest("Usuario ya está inscrito en esta oferta");

            var userOffer = new UserOffer
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                OfferId = dto.OfferId,
                LiberatedAmount = 0,
                Status = OfferStatus.Activated
            };

            _dbContext.UserOffers.Add(userOffer);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        // POST: api/Offers/registerBet
        [HttpPost("registerBet")]
        public async Task<IActionResult> RegisterBet(RegisterBetDTO dto)
        {
            var userOffer = await _dbContext.UserOffers
                .Include(uo => uo.Offer)
                .FirstOrDefaultAsync(uo => uo.Id == dto.UserOfferId);

            if (userOffer == null)
                return NotFound("UserOffer no encontrado");

            if (userOffer.Status != OfferStatus.Activated)
                return BadRequest("La oferta no está activada o ya está completada");

            var bet = new Bet
            {
                Id = Guid.NewGuid(),
                UserOfferId = dto.UserOfferId,
                Amount = dto.Amount,
                DatePlaced = DateTime.UtcNow,
                HomeTeam = dto.HomeTeam,
                HomeTeamImg = dto.HomeTeamImg,
                AwayTeam = dto.AwayTeam,
                AwayTeamImg = dto.AwayTeamImg,
                Casino1 = dto.Casino1,
                AmountCasino1 = dto.AmountCasino1,
                Odds1 = dto.Odds1,
                CasinoX = dto.CasinoX,
                AmountCasinoX = dto.AmountCasinoX,
                OddsX = dto.OddsX,
                Casino2 = dto.Casino2,
                AmountCasino2 = dto.AmountCasino2,
                Odds2 = dto.Odds2
            };

            _dbContext.Bets.Add(bet);

            // Actualizar el progreso 
            userOffer.LiberatedAmount += dto.Amount;

            // Comprobar si ha alcanzado el máximo para liberar créditos
            if (userOffer.LiberatedAmount >= userOffer.Offer.MaxFreeAmount)
            {
                userOffer.Status = OfferStatus.CreditsReleased;
                var user = await _dbContext.Users.FindAsync(userOffer.UserId);
                if (user != null)
                {
                    user.FreeBets += userOffer.Offer.MaxFreeAmount;
                }
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { bet, userOffer });
        }


        // GET: api/Offers/userProgress/{userId}
        [HttpGet("userProgress/{userId}")]
        public async Task<IActionResult> GetUserProgress(Guid userId)
        {
            var userOffers = await _dbContext.UserOffers
                .Include(uo => uo.Offer)
                .Include(uo => uo.Bets)
                .Where(uo => uo.UserId == userId)
                .ToListAsync();

            if (userOffers == null || userOffers.Count == 0)
                return NotFound("No se encontraron ofertas para este usuario");

            return Ok(userOffers);
        }

        [HttpGet("isRegistered")]
        public async Task<IActionResult> IsUserRegistered(Guid userId, Guid offerId)
        {
            var isRegistered = await _dbContext.UserOffers
                .AnyAsync(uo => uo.UserId == userId && uo.OfferId == offerId);

            return Ok(isRegistered);  // Devuelve true o false
        }
        [HttpGet("getUserOfferId")]
        public async Task<IActionResult> GetUserOfferId(Guid userId, Guid offerId)
        {
            var userOffer = await _dbContext.UserOffers
                .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OfferId == offerId);

            if (userOffer == null)
                return NotFound("Relación UserOffer no encontrada");

            return Ok(userOffer.Id);
        }
        // GET: api/Offers/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOfferById(Guid id)
        {
            var offer = await _dbContext.Offers.FindAsync(id);
            if (offer == null)
                return NotFound("Oferta no encontrada");

            return Ok(offer);
        }
        // GET: api/Offers/userOffer?userId={userId}&offerId={offerId}
        [HttpGet("userOffer")]
        public async Task<IActionResult> GetUserOffer(Guid userId, Guid offerId)
        {
            var userOffer = await _dbContext.UserOffers
                .Include(uo => uo.Offer)
                .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OfferId == offerId);

            if (userOffer == null)
                return NotFound("Relación UserOffer no encontrada");

            return Ok(userOffer);
        }

        // GET: api/Offers/userBets/{userId}
        [HttpGet("userBets/{userId}")]
        public async Task<IActionResult> GetAllUserBets(Guid userId)
        {
            var userBets = await _dbContext.Bets
                .Include(b => b.UserOffer)
                    .ThenInclude(uo => uo.Offer)
                .Where(b => b.UserOffer.UserId == userId)
                .OrderByDescending(b => b.DatePlaced)
                .ToListAsync();

            if (userBets == null || !userBets.Any())
                return NotFound("No se encontraron apuestas para este usuario");

            return Ok(userBets);
        }



    }
}
