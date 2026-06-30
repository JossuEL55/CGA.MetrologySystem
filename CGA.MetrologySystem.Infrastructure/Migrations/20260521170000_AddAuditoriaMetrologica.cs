using System;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260521170000_AddAuditoriaMetrologica")]
    public partial class AddAuditoriaMetrologica : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriasMetrologicas",
                columns: table => new
                {
                    AuditoriaMetrologicaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    UsuarioCorreo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RolUsuario = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Accion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Entidad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntidadId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EquipoId = table.Column<int>(type: "integer", nullable: true),
                    CodigoEquipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NombreEquipo = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: true),
                    TipoEvento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EsCritico = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriasMetrologicas", x => x.AuditoriaMetrologicaId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriasMetrologicas");
        }
    }
}
