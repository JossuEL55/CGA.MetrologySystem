using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenciasEventoMetrologico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvidenciasEventoMetrologico",
                columns: table => new
                {
                    EvidenciaEventoMetrologicoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    NombreArchivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GoogleDriveFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RutaArchivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoEvidencia = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCarga = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenciasEventoMetrologico", x => x.EvidenciaEventoMetrologicoId);
                    table.ForeignKey(
                        name: "FK_EvidenciasEventoMetrologico_EventosMetrologicos_EventoMetro~",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciasEventoMetrologico_EventoMetrologicoId",
                table: "EvidenciasEventoMetrologico",
                column: "EventoMetrologicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenciasEventoMetrologico");
        }
    }
}
