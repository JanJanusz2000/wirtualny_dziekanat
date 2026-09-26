using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wirtualny_dziekanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectSemesterAndStudentCurrentSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Semestr",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AktualnySemestr",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Semestr",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "AktualnySemestr",
                table: "Students");
        }
    }
}
