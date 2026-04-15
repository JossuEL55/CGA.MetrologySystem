using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratorioEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    EquipoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    TipoEquipoId = table.Column<int>(type: "integer", nullable: false),
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    UbicacionId = table.Column<int>(type: "integer", nullable: false),
                    ResponsableInternoId = table.Column<int>(type: "integer", nullable: false),
                    Marca = table.Column<string>(type: "text", nullable: true),
                    Modelo = table.Column<string>(type: "text", nullable: true),
                    Serie = table.Column<string>(type: "text", nullable: true),
                    Identificacion = table.Column<string>(type: "text", nullable: true),
                    FechaAdquisicion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaPuestaFuncionamiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FabricanteLugarOrigen = table.Column<string>(type: "text", nullable: true),
                    CatalogoManejoOperacion = table.Column<string>(type: "text", nullable: true),
                    MantenimientoFabricante = table.Column<string>(type: "text", nullable: true),
                    CondicionesOperacion = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.EquipoId);
                    table.ForeignKey(
                        name: "FK_Equipos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "ProveedorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipos_ResponsablesInternos_ResponsableInternoId",
                        column: x => x.ResponsableInternoId,
                        principalTable: "ResponsablesInternos",
                        principalColumn: "ResponsableInternoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipos_TiposEquipo_TipoEquipoId",
                        column: x => x.TipoEquipoId,
                        principalTable: "TiposEquipo",
                        principalColumn: "TipoEquipoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipos_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "Ubicaciones",
                        principalColumn: "UbicacionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Laboratorios",
                columns: table => new
                {
                    LaboratorioId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Ciudad = table.Column<string>(type: "text", nullable: true),
                    Pais = table.Column<string>(type: "text", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    SitioWeb = table.Column<string>(type: "text", nullable: true),
                    NormaAcreditacion = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laboratorios", x => x.LaboratorioId);
                });

            migrationBuilder.CreateTable(
                name: "CaracteristicasMetrologicasEquipo",
                columns: table => new
                {
                    CaracteristicaMetrologicaEquipoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: true),
                    Unidad = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaracteristicasMetrologicasEquipo", x => x.CaracteristicaMetrologicaEquipoId);
                    table.ForeignKey(
                        name: "FK_CaracteristicasMetrologicasEquipo_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "EquipoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesControlEquipo",
                columns: table => new
                {
                    ConfiguracionControlEquipoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipoId = table.Column<int>(type: "integer", nullable: false),
                    TipoEventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    PeriodicidadValor = table.Column<int>(type: "integer", nullable: true),
                    PeriodicidadUnidad = table.Column<string>(type: "text", nullable: true),
                    RequiereControl = table.Column<bool>(type: "boolean", nullable: false),
                    PermitePorIngreso = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesControlEquipo", x => x.ConfiguracionControlEquipoId);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesControlEquipo_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "EquipoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesControlEquipo_TiposEventoMetrologico_TipoEve~",
                        column: x => x.TipoEventoMetrologicoId,
                        principalTable: "TiposEventoMetrologico",
                        principalColumn: "TipoEventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventosMetrologicos",
                columns: table => new
                {
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipoId = table.Column<int>(type: "integer", nullable: false),
                    TipoEventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    SubtipoEventoId = table.Column<int>(type: "integer", nullable: false),
                    ResponsableInternoId = table.Column<int>(type: "integer", nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaProxima = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstadoEquipoResultado = table.Column<string>(type: "text", nullable: true),
                    ComentariosAdicionales = table.Column<string>(type: "text", nullable: true),
                    EsExtraordinario = table.Column<bool>(type: "boolean", nullable: false),
                    JustificacionExtraordinario = table.Column<string>(type: "text", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosMetrologicos", x => x.EventoMetrologicoId);
                    table.ForeignKey(
                        name: "FK_EventosMetrologicos_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "EquipoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventosMetrologicos_ResponsablesInternos_ResponsableInterno~",
                        column: x => x.ResponsableInternoId,
                        principalTable: "ResponsablesInternos",
                        principalColumn: "ResponsableInternoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventosMetrologicos_SubtiposEvento_SubtipoEventoId",
                        column: x => x.SubtipoEventoId,
                        principalTable: "SubtiposEvento",
                        principalColumn: "SubtipoEventoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventosMetrologicos_TiposEventoMetrologico_TipoEventoMetrol~",
                        column: x => x.TipoEventoMetrologicoId,
                        principalTable: "TiposEventoMetrologico",
                        principalColumn: "TipoEventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventosCalibracionDato",
                columns: table => new
                {
                    EventoCalibracionDatoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroCertificado = table.Column<string>(type: "text", nullable: true),
                    FechaCalibracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LaboratorioId = table.Column<int>(type: "integer", nullable: true),
                    RutaCertificado = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosCalibracionDato", x => x.EventoCalibracionDatoId);
                    table.ForeignKey(
                        name: "FK_EventosCalibracionDato_EventosMetrologicos_EventoMetrologic~",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventosCalibracionDato_Laboratorios_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "Laboratorios",
                        principalColumn: "LaboratorioId");
                });

            migrationBuilder.CreateTable(
                name: "EventosMantenimientoActividad",
                columns: table => new
                {
                    EventoMantenimientoActividadId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    DescripcionActividad = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosMantenimientoActividad", x => x.EventoMantenimientoActividadId);
                    table.ForeignKey(
                        name: "FK_EventosMantenimientoActividad_EventosMetrologicos_EventoMet~",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventosVerificacionResultado",
                columns: table => new
                {
                    EventoVerificacionResultadoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoMetrologicoId = table.Column<int>(type: "integer", nullable: false),
                    DescripcionItem = table.Column<string>(type: "text", nullable: false),
                    Cumple = table.Column<bool>(type: "boolean", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosVerificacionResultado", x => x.EventoVerificacionResultadoId);
                    table.ForeignKey(
                        name: "FK_EventosVerificacionResultado_EventosMetrologicos_EventoMetr~",
                        column: x => x.EventoMetrologicoId,
                        principalTable: "EventosMetrologicos",
                        principalColumn: "EventoMetrologicoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaracteristicasMetrologicasEquipo_EquipoId",
                table: "CaracteristicasMetrologicasEquipo",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesControlEquipo_EquipoId",
                table: "ConfiguracionesControlEquipo",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesControlEquipo_TipoEventoMetrologicoId",
                table: "ConfiguracionesControlEquipo",
                column: "TipoEventoMetrologicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_ProveedorId",
                table: "Equipos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_ResponsableInternoId",
                table: "Equipos",
                column: "ResponsableInternoId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_TipoEquipoId",
                table: "Equipos",
                column: "TipoEquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_UbicacionId",
                table: "Equipos",
                column: "UbicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCalibracionDato_EventoMetrologicoId",
                table: "EventosCalibracionDato",
                column: "EventoMetrologicoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosCalibracionDato_LaboratorioId",
                table: "EventosCalibracionDato",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosMantenimientoActividad_EventoMetrologicoId",
                table: "EventosMantenimientoActividad",
                column: "EventoMetrologicoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosMetrologicos_EquipoId",
                table: "EventosMetrologicos",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosMetrologicos_ResponsableInternoId",
                table: "EventosMetrologicos",
                column: "ResponsableInternoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosMetrologicos_SubtipoEventoId",
                table: "EventosMetrologicos",
                column: "SubtipoEventoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosMetrologicos_TipoEventoMetrologicoId",
                table: "EventosMetrologicos",
                column: "TipoEventoMetrologicoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosVerificacionResultado_EventoMetrologicoId",
                table: "EventosVerificacionResultado",
                column: "EventoMetrologicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaracteristicasMetrologicasEquipo");

            migrationBuilder.DropTable(
                name: "ConfiguracionesControlEquipo");

            migrationBuilder.DropTable(
                name: "EventosCalibracionDato");

            migrationBuilder.DropTable(
                name: "EventosMantenimientoActividad");

            migrationBuilder.DropTable(
                name: "EventosVerificacionResultado");

            migrationBuilder.DropTable(
                name: "Laboratorios");

            migrationBuilder.DropTable(
                name: "EventosMetrologicos");

            migrationBuilder.DropTable(
                name: "Equipos");
        }
    }
}
