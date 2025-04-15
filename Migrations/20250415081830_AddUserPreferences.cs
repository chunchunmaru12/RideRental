using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideRental.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredCategory",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredEngineType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PreferredMinPower",
                table: "Users",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredCategory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredEngineType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredMinPower",
                table: "Users");
        }
    }
}
