using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wirtualny_dziekanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectEctsAndDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KierunekId",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PunktyECTS",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Uzupełnienie danych istniejących przed dodaniem wymaganego klucza obcego.
            migrationBuilder.Sql("""
                UPDATE Subjects
                SET KierunekId = (
                    SELECT TOP (1) Id
                    FROM Kierunki
                    WHERE Nazwa = N'Informatyka'
                )
                WHERE KierunekId = 0;

                UPDATE Subjects
                SET PunktyECTS = 5
                WHERE PunktyECTS = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_KierunekId",
                table: "Subjects",
                column: "KierunekId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Kierunki_KierunekId",
                table: "Subjects",
                column: "KierunekId",
                principalTable: "Kierunki",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Kierunki_KierunekId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_KierunekId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "KierunekId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "PunktyECTS",
                table: "Subjects");
        }
    }
}
