using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalibracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleDriveFileId",
                table: "EventosCalibracionDato",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivoCertificado",
                table: "EventosCalibracionDato",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleDriveFileId",
                table: "EventosCalibracionDato");

            migrationBuilder.DropColumn(
                name: "NombreArchivoCertificado",
                table: "EventosCalibracionDato");
        }
    }
}
