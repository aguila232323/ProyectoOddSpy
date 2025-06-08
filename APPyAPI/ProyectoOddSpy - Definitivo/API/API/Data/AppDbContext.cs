using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<UserOffer> UserOffers { get; set; }
        public DbSet<Bet> Bets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<UserOffer>()
                .Property(uo => uo.Status)
                .HasConversion<string>();

            modelBuilder.Entity<UserOffer>()
                .HasOne(uo => uo.User)
                .WithMany(u => u.UserOffers)
                .HasForeignKey(uo => uo.UserId);

            modelBuilder.Entity<UserOffer>()
                .HasOne(uo => uo.Offer)
                .WithMany(o => o.UserOffers)
                .HasForeignKey(uo => uo.OfferId);

            modelBuilder.Entity<Bet>()
                .HasOne(b => b.UserOffer)
                .WithMany(uo => uo.Bets)
                .HasForeignKey(b => b.UserOfferId);

            // Datos por defecto: ofertas iniciales
            modelBuilder.Entity<Offer>().HasData(
                new Offer
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Casino = "BetFair",
                    BannerImg = "codigo_bono_betfair.png",
                    Descryption = "📈 Cuota mínima 1.5\n\n🆕 Solo está disponible para nuevos usuarios registrados en www.betfair.es con el código promocional ZBBES2\n\n⏰ Tendrás 7 días desde que te acreditemos tu apuesta gratuita para utilizarla\n\n🎯 Podrás utilizar tu apuesta gratuita en cualquier evento",
                    Title = "Hasta 200€ en Apuestas Gratis",
                    MaxFreeAmount = 200,
                    Type = "Bono Bienvenida🎁"
                },
                new Offer
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Casino = "Bwin",
                    BannerImg = "codigo_bono_bwin.png",
                    Descryption = "🎯 Solo será válida la primera apuesta realizada después de registrarte\n\n💶 La primera apuesta tras registrarte (mínimo 1 €, saldo real, no virtuales) se devuelve al 100% (hasta 100 €) como Apuesta Gratuita si la pierdes\n\n⏳ Las Apuestas Gratuitas y Apuestas Seguras que te den tienen 7 días para usarse y solo devuelven la ganancia neta\n\n📅 Apuesta válida dentro de 30 días desde el registro, sin usar bonos ni supercuotas",
                    Title = "Hasta 100€ asegurados en tu 1ª apuesta",
                    MaxFreeAmount = 100,
                    Type = "Bono Bienvenida🎁"
                },
                new Offer
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Casino = "Interwetten",
                    BannerImg = "codigo_bono_interwetten.png",
                    Descryption = "💰 El importe máximo para la apuesta sin riesgo es de 50 €\n\n🎯 Una apuesta de 40 € fallada otorga una apuesta gratuita de 40 €\n\n🎯 Una apuesta de 60 € fallada otorga una apuesta gratuita de 50 €\n\n🔢 El importe mínimo de apuesta debe ser de 10 €\n\n❌ Solo se tendrán en cuenta las apuestas falladas\n\n⚽ Pueden realizar apuestas simples o combinadas, live o prepartido para esta promoción",
                    Title = "¡Tu primera apuesta sin riesgo hasta 50€!",
                    MaxFreeAmount = 50,
                    Type = "Bono Bienvenida🎁"
                }
            );
        }

    }
}
