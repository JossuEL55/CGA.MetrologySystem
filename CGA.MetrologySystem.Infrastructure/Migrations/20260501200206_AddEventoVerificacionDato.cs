using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoVerificacionDato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosVerificacionDato",
                columns: table => new
                {
                    EventoVerificacionDatoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    GoogleDriveFileId = table.Column<string>(type: "text", nullable: true),
                    NombreArchivoPdf = table.Column<string>(type: "text", nullable: true),
                    RutaPdf = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosVerificacionDato", x => x.EventoVerificacionDatoId);
                    table.ForeignKey(
                        name: "FK_EventosVerificacionDato_EventosMetrologicos_EventoMetrologi~",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosVerificacionDato_EventoMetrologicoId",
                table: "EventosVerificacionDato",
                column: "EventoMetrologicoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosVerificacionDato");
        }
    }
}
