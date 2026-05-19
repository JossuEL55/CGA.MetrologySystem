using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFichaTecnicaEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FichasTecnicasEquipo",
                columns: table => new
                {
                    FichaTecnicaEquipoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipoId = table.Column<int>(type: "integer", nullable: false),
                    NombreArchivoPdf = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    GoogleDriveFileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RutaPdf = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaUltimaGeneracion = table.Column<DateTime>(type: "date", nullable: false),
                    CantidadEventosIncluidos = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasTecnicasEquipo", x => x.FichaTecnicaEquipoId);
                    table.ForeignKey(
                        name: "FK_FichasTecnicasEquipo_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "EquipoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicasEquipo_EquipoId",
                table: "FichasTecnicasEquipo",
                column: "EquipoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FichasTecnicasEquipo");
        }
    }
}
