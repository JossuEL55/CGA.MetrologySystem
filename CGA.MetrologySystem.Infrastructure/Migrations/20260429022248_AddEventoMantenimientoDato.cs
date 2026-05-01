using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoMantenimientoDato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosMantenimientoDato",
                columns: table => new
                {
                    EventoMantenimientoDatoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    TipoMantenimientoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosMantenimientoDato", x => x.EventoMantenimientoDatoId);
                    table.ForeignKey(
                        name: "FK_EventosMantenimientoDato_EventosMetrologicos_EventoMetrolog~",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventosMantenimientoDato_TiposMantenimiento_TipoMantenimien~",
                        column: x => x.TipoMantenimientoId,
                        principalTable: "TiposMantenimiento",
                        principalColumn: "TipoMantenimientoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosMantenimientoDato_EventoMetrologicoId",
                table: "EventosMantenimientoDato",
                column: "EventoMetrologicoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosMantenimientoDato_TipoMantenimientoId",
                table: "EventosMantenimientoDato",
                column: "TipoMantenimientoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosMantenimientoDato");
        }
    }
}
