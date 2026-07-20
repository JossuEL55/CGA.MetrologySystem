using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.TendenciasMetrologicas;
using CGA.MetrologySystem.Services.TendenciasMetrologicas;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class TendenciasMetrologicasServiceTests
{
    [Fact]
    public async Task ObtenerIndexAsync_SinEventos_RetornaResumenVacio()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel());

        Assert.Empty(vista.Equipos);
        Assert.Equal(0, vista.Resumen.EventosAnalizados);
        Assert.Equal(0, vista.Resumen.DesviacionPromedioGlobal);
        Assert.Equal(2, vista.Filtros.TiposEquipo.Count);
        Assert.Equal(2, vista.Filtros.TiposEvento.Count);
    }

    [Fact]
    public async Task ObtenerIndexAsync_UnSoloEvento_NoGeneraDesviacion()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Balanza");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel());

        Assert.Empty(vista.Equipos);
        Assert.Equal(0, vista.Resumen.EventosAnalizados);
    }

    [Fact]
    public async Task ObtenerIndexAsync_EventoEnFechaEsperada_ClasificaATiempo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Balanza");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1));
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel());

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(1, equipo.EventosAnalizados);
        Assert.Equal(0, equipo.DesviacionPromedio);
        Assert.Equal(1, vista.Resumen.EventosATiempo);
        Assert.Equal(0, vista.Resumen.EventosTardios);
    }

    [Fact]
    public async Task ObtenerIndexAsync_EventosTardioYAnticipado_CalculaResumenCompleto()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Balanza");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 2, 6), new DateTime(2026, 3, 6));
        SeedEvento(
            context,
            1,
            1,
            new DateTime(2026, 3, 3),
            new DateTime(2026, 4, 3),
            extraordinario: true,
            justificacion: "Ajuste operativo");
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel());

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(2, equipo.EventosAnalizados);
        Assert.Equal(1, equipo.EventosTardios);
        Assert.Equal(1, equipo.EventosExtraordinarios);
        Assert.Equal(1, vista.Resumen.EventosAnticipados);
        Assert.Equal(1, vista.Resumen.EventosTardios);
        Assert.Equal(5, vista.Resumen.MayorDesviacion);
        Assert.Equal(1, vista.Resumen.DesviacionPromedioGlobal);
    }

    [Fact]
    public async Task ObtenerIndexAsync_EventoInactivo_NoParticipaEnCalculo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Balanza");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 2, 10), null, activo: false);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel());

        Assert.Empty(vista.Equipos);
        Assert.Equal(0, vista.Resumen.EventosAnalizados);
    }

    [Fact]
    public async Task ObtenerIndexAsync_EventoPrevioSinFechaProxima_OmiteSoloEseIntervalo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Balanza");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), null);
        SeedEvento(context, 1, 1, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 3, 4), new DateTime(2026, 4, 4));
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel());

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(1, equipo.EventosAnalizados);
        Assert.Equal(3, equipo.DesviacionPromedio);
    }

    [Fact]
    public async Task ObtenerIndexAsync_BusquedaYTipoEquipo_FiltranSinDistinguirMayusculas()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "BAL-001", "Balanza analítica", tipoEquipoId: 1);
        SeedEquipo(context, 2, "TER-001", "Termómetro patrón", tipoEquipoId: 2);
        SeedParConDesviacion(context, 1, 1, 2);
        SeedParConDesviacion(context, 2, 1, 8);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel
        {
            Buscar = "  TERMÓMETRO ",
            TipoEquipoId = 2
        });

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal("TER-001", equipo.CodigoEquipo);
        Assert.Contains(vista.Filtros.TiposEquipo, t => t.Value == "2" && t.Selected);
    }

    [Fact]
    public async Task ObtenerIndexAsync_FiltroTipoEvento_DevuelveSoloControlSeleccionado()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Equipo mixto");
        SeedParConDesviacion(context, 1, 1, 3);
        SeedParConDesviacion(context, 1, 2, 9, mesInicial: 4);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new TendenciasMetrologicasFiltroViewModel
        {
            TipoEventoMetrologicoId = 2
        });

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(1, equipo.EventosAnalizados);
        Assert.Equal(9, equipo.DesviacionPromedio);
        Assert.Contains(vista.Filtros.TiposEvento, t => t.Value == "2" && t.Selected);
    }

    [Fact]
    public async Task ObtenerIndexAsync_RangoInvertido_IntercambiaFechasYAplicaFiltroInclusivo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-001", "Equipo con historial");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 2, 2), new DateTime(2026, 3, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 3, 2), new DateTime(2026, 4, 1));
        await context.SaveChangesAsync();
        var filtros = new TendenciasMetrologicasFiltroViewModel
        {
            FechaDesde = new DateTime(2026, 3, 2),
            FechaHasta = new DateTime(2026, 2, 2)
        };

        var vista = await CrearServicio(context).ObtenerIndexAsync(filtros);

        Assert.Equal(new DateTime(2026, 2, 2), vista.Filtros.FechaDesde);
        Assert.Equal(new DateTime(2026, 3, 2), vista.Filtros.FechaHasta);
        Assert.Equal(2, vista.Resumen.EventosAnalizados);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_EquipoInexistente_RetornaNull()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        await context.SaveChangesAsync();

        var detalle = await CrearServicio(context).ObtenerDetalleAsync(
            999,
            new TendenciasMetrologicasFiltroViewModel());

        Assert.Null(detalle);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_EquipoExistente_OrdenaDesviacionesDeMasRecienteAMasAntigua()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-DET", "Equipo detalle");
        SeedEvento(context, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 2, 2), new DateTime(2026, 3, 1));
        SeedEvento(context, 1, 1, new DateTime(2026, 3, 3), new DateTime(2026, 4, 1));
        await context.SaveChangesAsync();

        var detalle = await CrearServicio(context).ObtenerDetalleAsync(
            1,
            new TendenciasMetrologicasFiltroViewModel { Buscar = "ignorado", TipoEquipoId = 2 });

        Assert.NotNull(detalle);
        Assert.Equal("EQ-DET", detalle.CodigoEquipo);
        Assert.Equal(2, detalle.Desviaciones.Count);
        Assert.True(detalle.Desviaciones[0].FechaEvento > detalle.Desviaciones[1].FechaEvento);
        Assert.Null(detalle.Filtros.Buscar);
        Assert.Null(detalle.Filtros.TipoEquipoId);
    }

    private static TendenciasMetrologicasService CrearServicio(AppDbContext context) => new(context);

    private static void SeedCatalogos(AppDbContext context)
    {
        context.TiposEquipo.AddRange(
            new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" },
            new TipoEquipo { TipoEquipoId = 2, Nombre = "Temperatura" });
        context.TiposEventoMetrologico.AddRange(
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 1, Nombre = "Calibración" },
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 2, Nombre = "Verificación" });
        context.SubtiposEvento.Add(new SubtipoEvento
        {
            SubtipoEventoId = 1,
            Nombre = "Periódico",
            Activo = true
        });
    }

    private static void SeedEquipo(
        AppDbContext context,
        int id,
        string codigo,
        string nombre,
        int tipoEquipoId = 1)
    {
        context.Equipos.Add(new Equipo
        {
            EquipoId = id,
            Codigo = codigo,
            Nombre = nombre,
            TipoEquipoId = tipoEquipoId,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        });
    }

    private static void SeedParConDesviacion(
        AppDbContext context,
        int equipoId,
        int tipoEventoId,
        int diasDesviacion,
        int mesInicial = 1)
    {
        var fechaInicial = new DateTime(2026, mesInicial, 1);
        var fechaEsperada = fechaInicial.AddMonths(1);
        SeedEvento(context, equipoId, tipoEventoId, fechaInicial, fechaEsperada);
        SeedEvento(
            context,
            equipoId,
            tipoEventoId,
            fechaEsperada.AddDays(diasDesviacion),
            fechaEsperada.AddMonths(1));
    }

    private static void SeedEvento(
        AppDbContext context,
        int equipoId,
        int tipoEventoId,
        DateTime fechaEvento,
        DateTime? fechaProxima,
        bool extraordinario = false,
        string? justificacion = null,
        bool activo = true)
    {
        context.EventosMetrologicos.Add(new EventoMetrologico
        {
            EquipoId = equipoId,
            TipoEventoMetrologicoId = tipoEventoId,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = fechaEvento.Date,
            FechaProxima = fechaProxima?.Date,
            EsExtraordinario = extraordinario,
            JustificacionExtraordinario = justificacion,
            Activo = activo
        });
    }
}
