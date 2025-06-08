using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AñadirDescriptionAUserOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiberatedAmount",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "IsSettled",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "IsSureBet",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "IsWon",
                table: "Bets");

            migrationBuilder.AddColumn<decimal>(
                name: "FreeBets",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountCasino1",
                table: "Bets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountCasino2",
                table: "Bets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountCasinoX",
                table: "Bets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AwayTeam",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AwayTeamImg",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Casino1",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Casino2",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CasinoX",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeTeam",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeTeamImg",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Odds1",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Odds2",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OddsX",
                table: "Bets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeBets",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AmountCasino1",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "AmountCasino2",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "AmountCasinoX",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "AwayTeam",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "AwayTeamImg",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "Casino1",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "Casino2",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "CasinoX",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "HomeTeam",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "HomeTeamImg",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "Odds1",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "Odds2",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "OddsX",
                table: "Bets");

            migrationBuilder.AddColumn<int>(
                name: "LiberatedAmount",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSettled",
                table: "Bets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSureBet",
                table: "Bets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWon",
                table: "Bets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Offers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "LiberatedAmount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Offers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "LiberatedAmount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Offers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "LiberatedAmount",
                value: 0);
        }
    }
}
