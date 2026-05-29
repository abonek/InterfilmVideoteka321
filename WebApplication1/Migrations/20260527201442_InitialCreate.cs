using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filmovi",
                columns: table => new
                {
                    IdFilma = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NazivFilma = table.Column<string>(type: "TEXT", nullable: false),
                    CijenaKupnje = table.Column<float>(type: "REAL", nullable: false),
                    CijenaRente = table.Column<float>(type: "REAL", nullable: false),
                    DatumRente = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Rezervacija = table.Column<int>(type: "INTEGER", nullable: false),
                    IdKupca = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filmovi", x => x.IdFilma);
                });

            migrationBuilder.CreateTable(
                name: "iznajmljivanja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilmId = table.Column<int>(type: "INTEGER", nullable: false),
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: false),
                    NazivFilma = table.Column<string>(type: "TEXT", nullable: false),
                    CijenaRente = table.Column<float>(type: "REAL", nullable: false),
                    DatumPocetka = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumPodizanja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DatumPovrata = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipRezervacije = table.Column<int>(type: "INTEGER", nullable: false),
                    Vratio = table.Column<bool>(type: "INTEGER", nullable: false),
                    Podignut = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iznajmljivanja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "korisnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_korisnici", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_korisnici_Email",
                table: "korisnici",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filmovi");

            migrationBuilder.DropTable(
                name: "iznajmljivanja");

            migrationBuilder.DropTable(
                name: "korisnici");
        }
    }
}
