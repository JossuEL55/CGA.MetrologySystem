using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantillasEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantillasEvento",
                columns: table => new
                {
                    PlantillaEventoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoEquipoId = table.Column<int>(type: "integer", nullable: false),
                    TipoEventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasEvento", x => x.PlantillaEventoId);
                    table.ForeignKey(
                        name: "FK_PlantillasEvento_TiposEquipo_TipoEquipoId",
                        column: x => x.TipoEquipoId,
                        principalTable: "TiposEquipo",
                        principalColumn: "TipoEquipoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantillasEvento_TiposEventoMetrologico_TipoEventoMetrologi~",
                        column: x => x.TipoEventoMetrologicoId,
                        principalTable: "TiposEventoMetrologico",
                        principalColumn: "TipoEventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantillasEventoItem",
                columns: table => new
                {
                    PlantillaEventoItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantillaEventoId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasEventoItem", x => x.PlantillaEventoItemId);
                    table.ForeignKey(
                        name: "FK_PlantillasEventoItem_PlantillasEvento_PlantillaEventoId",
                        column: x => x.PlantillaEventoId,
                        principalTable: "PlantillasEvento",
                        principalColumn: "PlantillaEventoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasEvento_TipoEquipoId",
                table: "PlantillasEvento",
                column: "TipoEquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasEvento_TipoEventoMetrologicoId",
                table: "PlantillasEvento",
                column: "TipoEventoMetrologicoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasEventoItem_PlantillaEventoId",
                table: "PlantillasEventoItem",
                column: "PlantillaEventoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantillasEventoItem");

            migrationBuilder.DropTable(
                name: "PlantillasEvento");
        }
    }
}
