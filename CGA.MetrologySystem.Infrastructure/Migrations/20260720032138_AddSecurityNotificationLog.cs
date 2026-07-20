using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificacionesSeguridadEnviadas",
                columns: table => new
                {
                    NotificacionSeguridadEnviadaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoEvento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UsuarioAfectadoId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UsuarioAfectadoCorreo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UsuarioEjecutorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UsuarioEjecutorCorreo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Destinatarios = table.Column<string>(type: "text", nullable: true),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FueExitosa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesSeguridadEnviadas", x => x.NotificacionSeguridadEnviadaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacionesSeguridadEnviadas");
        }
    }
}
