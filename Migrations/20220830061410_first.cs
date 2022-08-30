using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feralas.Migrations
{
    public partial class first : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WowAuctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartitionKey = table.Column<string>(type: "TEXT", nullable: true),
                    AuctionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstSeenTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ShortTimeLeftSeen = table.Column<bool>(type: "INTEGER", nullable: false),
                    Sold = table.Column<bool>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    Buyout = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WowAuctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WowItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    BonusList = table.Column<string>(type: "TEXT", nullable: true),
                    PetBreedId = table.Column<int>(type: "INTEGER", nullable: true),
                    PetLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    PetQualityId = table.Column<int>(type: "INTEGER", nullable: true),
                    PetSpeciesId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WowItems", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WowAuctions");

            migrationBuilder.DropTable(
                name: "WowItems");
        }
    }
}
