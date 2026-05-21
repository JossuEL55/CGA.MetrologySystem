using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521193000_AddAnulacionVerificacionBase")]
    public partial class AddAnulacionVerificacionBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Anulado",
                table: "EventosMetrologicos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "EventosMetrologicos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "EventosMetrologicos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAnulacionId",
                table: "EventosMetrologicos",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAnulacionNombre",
                table: "EventosMetrologicos",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anulado",
                table: "EventosMetrologicos");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "EventosMetrologicos");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "EventosMetrologicos");

            migrationBuilder.DropColumn(
                name: "UsuarioAnulacionId",
                table: "EventosMetrologicos");

            migrationBuilder.DropColumn(
                name: "UsuarioAnulacionNombre",
                table: "EventosMetrologicos");
        }
    }
}
