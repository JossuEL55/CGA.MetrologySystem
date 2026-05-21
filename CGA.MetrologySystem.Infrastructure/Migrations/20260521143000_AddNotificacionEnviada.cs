using System;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521143000_AddNotificacionEnviada")]
    public partial class AddNotificacionEnviada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificacionesEnviadas",
                columns: table => new
                {
                    NotificacionEnviadaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipoId = table.Column<int>(type: "integer", nullable: false),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    TipoNotificacion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TipoEvento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaReferencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Destinatarios = table.Column<string>(type: "text", nullable: true),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FueExitosa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesEnviadas", x => x.NotificacionEnviadaId);
                    table.ForeignKey(
                        name: "FK_NotificacionesEnviadas_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "EquipoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacionesEnviadas_EventosMetrologicos_EventoMetrologicoId",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesEnviadas_EquipoId",
                table: "NotificacionesEnviadas",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesEnviadas_EventoMetrologicoId",
                table: "NotificacionesEnviadas",
                column: "EventoMetrologicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacionesEnviadas");
        }
    }
}
