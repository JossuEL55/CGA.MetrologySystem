using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfFieldsToEventoMantenimientoDato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleDriveFileId",
                table: "EventosMantenimientoDato",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivoPdf",
                table: "EventosMantenimientoDato",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaPdf",
                table: "EventosMantenimientoDato",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleDriveFileId",
                table: "EventosMantenimientoDato");

            migrationBuilder.DropColumn(
                name: "NombreArchivoPdf",
                table: "EventosMantenimientoDato");

            migrationBuilder.DropColumn(
                name: "RutaPdf",
                table: "EventosMantenimientoDato");
        }
    }
}
