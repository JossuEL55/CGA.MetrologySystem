using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposLaboratorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alcance",
                table: "Laboratorios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroAcreditacion",
                table: "Laboratorios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Laboratorios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alcance",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "NumeroAcreditacion",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Laboratorios");
        }
    }
}
