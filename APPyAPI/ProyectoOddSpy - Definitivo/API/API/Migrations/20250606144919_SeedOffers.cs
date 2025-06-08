using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class SeedOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Offers",
                newName: "Descryption");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Bets",
                newName: "DatePlaced");

            migrationBuilder.AlterColumn<int>(
                name: "MaxFreeAmount",
                table: "Offers",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "LiberatedAmount",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Offers",
                columns: new[] { "Id", "BannerImg", "Casino", "Descryption", "LiberatedAmount", "MaxFreeAmount", "Title", "Type" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "codigo_bono_betfair.png", "BetFair", "📈 Cuota mínima 1.5\n\n🆕 Solo está disponible para nuevos usuarios registrados en www.betfair.es con el código promocional ZBBES2\n\n⏰ Tendrás 7 días desde que te acreditemos tu apuesta gratuita para utilizarla\n\n🎯 Podrás utilizar tu apuesta gratuita en cualquier evento", 0, 200, "Hasta 200€ en Apuestas Gratis", "Bono Bienvenida🎁" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "codigo_bono_bwin.png", "Bwin", "🎯 Solo será válida la primera apuesta realizada después de registrarte\n\n💶 La primera apuesta tras registrarte (mínimo 1 €, saldo real, no virtuales) se devuelve al 100% (hasta 100 €) como Apuesta Gratuita si la pierdes\n\n⏳ Las Apuestas Gratuitas y Apuestas Seguras que te den tienen 7 días para usarse y solo devuelven la ganancia neta\n\n📅 Apuesta válida dentro de 30 días desde el registro, sin usar bonos ni supercuotas", 0, 100, "Hasta 100€ asegurados en tu 1ª apuesta", "Bono Bienvenida🎁" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "codigo_bono_interwetten.png", "Interwetten", "💰 El importe máximo para la apuesta sin riesgo es de 50 €\n\n🎯 Una apuesta de 40 € fallada otorga una apuesta gratuita de 40 €\n\n🎯 Una apuesta de 60 € fallada otorga una apuesta gratuita de 50 €\n\n🔢 El importe mínimo de apuesta debe ser de 10 €\n\n❌ Solo se tendrán en cuenta las apuestas falladas\n\n⚽ Pueden realizar apuestas simples o combinadas, live o prepartido para esta promoción", 0, 50, "¡Tu primera apuesta sin riesgo hasta 50€!", "Bono Bienvenida🎁" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Offers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Offers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Offers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "LiberatedAmount",
                table: "Offers");

            migrationBuilder.RenameColumn(
                name: "Descryption",
                table: "Offers",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DatePlaced",
                table: "Bets",
                newName: "Date");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxFreeAmount",
                table: "Offers",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
