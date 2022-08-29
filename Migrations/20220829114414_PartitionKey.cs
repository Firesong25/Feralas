using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feralas.Migrations
{
    public partial class PartitionKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartitionKey",
                table: "WowAuctions",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartitionKey",
                table: "WowAuctions");
        }
    }
}
