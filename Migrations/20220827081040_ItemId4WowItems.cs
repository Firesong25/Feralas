using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feralas.Migrations
{
    public partial class ItemId4WowItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WowItems",
                table: "WowItems");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "WowItems",
                newName: "ItemId");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "WowItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryKey",
                table: "WowItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WowItems",
                table: "WowItems",
                column: "PrimaryKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WowItems",
                table: "WowItems");

            migrationBuilder.DropColumn(
                name: "PrimaryKey",
                table: "WowItems");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "WowItems",
                newName: "Id");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "WowItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WowItems",
                table: "WowItems",
                column: "Id");
        }
    }
}
