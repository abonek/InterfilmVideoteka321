using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddKolicinaGodinaOtkazano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Otkazano",
                table: "iznajmljivanja",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GodinaSnimanja",
                table: "filmovi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kolicina",
                table: "filmovi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Otkazano",
                table: "iznajmljivanja");

            migrationBuilder.DropColumn(
                name: "GodinaSnimanja",
                table: "filmovi");

            migrationBuilder.DropColumn(
                name: "Kolicina",
                table: "filmovi");
        }
    }
}
