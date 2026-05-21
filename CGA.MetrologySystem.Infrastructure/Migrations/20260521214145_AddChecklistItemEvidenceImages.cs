using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistItemEvidenceImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenciaContentType",
                table: "EventosVerificacionResultado",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaGoogleDriveFileId",
                table: "EventosVerificacionResultado",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaNombreArchivo",
                table: "EventosVerificacionResultado",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaRutaArchivo",
                table: "EventosVerificacionResultado",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaContentType",
                table: "EventosMantenimientoActividad",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaGoogleDriveFileId",
                table: "EventosMantenimientoActividad",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaNombreArchivo",
                table: "EventosMantenimientoActividad",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaRutaArchivo",
                table: "EventosMantenimientoActividad",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenciaContentType",
                table: "EventosVerificacionResultado");

            migrationBuilder.DropColumn(
                name: "EvidenciaGoogleDriveFileId",
                table: "EventosVerificacionResultado");

            migrationBuilder.DropColumn(
                name: "EvidenciaNombreArchivo",
                table: "EventosVerificacionResultado");

            migrationBuilder.DropColumn(
                name: "EvidenciaRutaArchivo",
                table: "EventosVerificacionResultado");

            migrationBuilder.DropColumn(
                name: "EvidenciaContentType",
                table: "EventosMantenimientoActividad");

            migrationBuilder.DropColumn(
                name: "EvidenciaGoogleDriveFileId",
                table: "EventosMantenimientoActividad");

            migrationBuilder.DropColumn(
                name: "EvidenciaNombreArchivo",
                table: "EventosMantenimientoActividad");

            migrationBuilder.DropColumn(
                name: "EvidenciaRutaArchivo",
                table: "EventosMantenimientoActividad");
        }
    }
}
