using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wirtualny_dziekanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentDashboardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CzyCzesneOplacone",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TrybStudiow",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Dzienne");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CzyCzesneOplacone",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TrybStudiow",
                table: "Students");
        }
    }
}
