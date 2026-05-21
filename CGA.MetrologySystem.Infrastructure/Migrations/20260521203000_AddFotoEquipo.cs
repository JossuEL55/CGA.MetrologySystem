using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521203000_AddFotoEquipo")]
    public partial class AddFotoEquipo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoGoogleDriveFileId",
                table: "Equipos",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoNombreArchivo",
                table: "Equipos",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoRutaArchivo",
                table: "Equipos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoGoogleDriveFileId",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "FotoNombreArchivo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "FotoRutaArchivo",
                table: "Equipos");
        }
    }
}
