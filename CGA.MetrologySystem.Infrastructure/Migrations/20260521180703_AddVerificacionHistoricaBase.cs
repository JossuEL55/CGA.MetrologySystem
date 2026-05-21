using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521180703_AddVerificacionHistoricaBase")]
    public partial class AddVerificacionHistoricaBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsHistorico",
                table: "EventosMetrologicos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionCargaHistorica",
                table: "EventosMetrologicos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsHistorico",
                table: "EventosMetrologicos");

            migrationBuilder.DropColumn(
                name: "ObservacionCargaHistorica",
                table: "EventosMetrologicos");
        }
    }
}
