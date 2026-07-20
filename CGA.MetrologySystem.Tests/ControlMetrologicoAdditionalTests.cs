using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Services.ControlMetrologico;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class ControlMetrologicoAdditionalTests
{
    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_SinEquipos_RetornaResumenVacioYNormalizaHorizonte()
    {
        await using var context = TestDbContextFactory.Create();
        var service = new ControlMetrologicoService(context);
        var filtros = new ControlMetrologicoFiltroViewModel { HorizonteDias = 0 };

        var vista = await service.ObtenerVistaPorEquipoAsync(filtros);

        Assert.Empty(vista.Equipos);
        Assert.Equal(0, vista.Resumen.TotalEquipos);
        Assert.Equal(30, vista.Filtros.HorizonteDias);
        Assert.Contains(vista.Filtros.Horizontes, h => h.Value == "30" && h.Selected);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_EquipoInactivo_NoSeIncluye()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-INACTIVO", "Equipo inactivo", activo: false);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(NuevosFiltros());

        Assert.Empty(vista.Equipos);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_SinConfiguraciones_ClasificaTodosLosControles()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-SIN-CONFIG", "Sin configuración");
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(NuevosFiltros());

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(EstadoControlMetrologico.SinConfiguracion, equipo.EstadoGlobal);
        Assert.All(
            new[] { equipo.Calibracion, equipo.Verificacion, equipo.Mantenimiento },
            control => Assert.Equal(EstadoControlMetrologico.SinConfiguracion, control.Estado));
        Assert.Equal(1, vista.Resumen.SinConfiguracion);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_ControlesNoRequeridos_ClasificaEquipoCorrectamente()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-LIBRE", "No requiere control");
        SeedTresConfiguraciones(context, 1, requiereControl: false);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(NuevosFiltros());

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(EstadoControlMetrologico.NoRequiereControl, equipo.EstadoGlobal);
        Assert.Equal(1, vista.Resumen.NoRequierenControl);
        Assert.Contains("no requiere", equipo.Calibracion.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_ConfiguradoSinEventos_ClasificaSinEventos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-NUEVO", "Equipo nuevo");
        SeedTresConfiguraciones(context, 1);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(NuevosFiltros());

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(EstadoControlMetrologico.SinEventos, equipo.EstadoGlobal);
        Assert.Equal(1, vista.Resumen.SinEventos);
        Assert.Contains("No existen eventos", equipo.Verificacion.Mensaje);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_UltimoEventoSinFechaProxima_ConservaFechaYClasificaSinEventos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-SIN-FECHA", "Sin próxima fecha");
        SeedTresConfiguraciones(context, 1, requiereControl: false);
        SeedConfiguracion(context, 1, 1, requiereControl: true);
        var fechaEvento = DateTime.Today.AddDays(-10);
        SeedEvento(context, 1, 1, fechaEvento, null);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(NuevosFiltros());

        var control = Assert.Single(vista.Equipos).Calibracion;
        Assert.Equal(EstadoControlMetrologico.SinEventos, control.Estado);
        Assert.Equal(fechaEvento, control.FechaUltimoEvento);
        Assert.Null(control.FechaProxima);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_FechaProximaHoy_ClasificaProximoAVencerConCeroDias()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-HOY", "Vence hoy");
        SeedTresConfiguraciones(context, 1, requiereControl: false);
        SeedConfiguracion(context, 1, 1, requiereControl: true);
        SeedEvento(context, 1, 1, DateTime.Today.AddMonths(-1), DateTime.Today);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(NuevosFiltros());

        var control = Assert.Single(vista.Equipos).Calibracion;
        Assert.Equal(EstadoControlMetrologico.ProximoAVencer, control.Estado);
        Assert.Equal(0, control.DiasRestantes);
        Assert.Contains("0 día", control.Mensaje);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_BusquedaIgnoraMayusculasYEspacios()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "CGA-ALFA", "Balanza principal");
        SeedEquipo(context, 2, "CGA-BETA", "Termómetro auxiliar");
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30,
            Buscar = "  BALANZA  "
        });

        Assert.Equal("CGA-ALFA", Assert.Single(vista.Equipos).CodigoEquipo);
    }

    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_FiltraPorTipoEquipo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 2, Nombre = "Temperatura" });
        SeedEquipo(context, 1, "EQ-MASA", "Equipo masa", tipoEquipoId: 1);
        SeedEquipo(context, 2, "EQ-TEMP", "Equipo temperatura", tipoEquipoId: 2);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEquipoAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30,
            TipoEquipoId = 2
        });

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(2, equipo.TipoEquipoId);
        Assert.Contains(vista.Filtros.TiposEquipo, t => t.Value == "2" && t.Selected);
    }

    [Fact]
    public async Task ObtenerVistaPorEventoAsync_FiltraTipoYEstado()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-EVENTO", "Equipo por evento");
        SeedTresConfiguraciones(context, 1);
        SeedEvento(context, 1, 1, DateTime.Today.AddMonths(-1), DateTime.Today.AddDays(-1));
        SeedEvento(context, 1, 2, DateTime.Today.AddMonths(-1), DateTime.Today.AddDays(60));
        SeedEvento(context, 1, 3, DateTime.Today.AddMonths(-1), DateTime.Today.AddDays(10));
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaPorEventoAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30,
            TipoEventoMetrologicoId = 1,
            Estado = EstadoControlMetrologico.Vencido
        });

        var evento = Assert.Single(vista.Eventos);
        Assert.Equal("Calibración", evento.TipoEventoNombre);
        Assert.Equal(EstadoControlMetrologico.Vencido, evento.Estado);
        Assert.Equal(1, vista.Resumen.Vencidos);
    }

    [Fact]
    public async Task ObtenerVistaScoreAsync_SinEventos_AsignaPrioridadMedia()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-SCORE", "Sin eventos");
        SeedTresConfiguraciones(context, 1);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaScoreAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30,
            TipoEventoMetrologicoId = 1
        });

        var item = Assert.Single(vista.Items);
        Assert.Equal(45, item.ScoreMetrologico);
        Assert.Equal("Medio", item.NivelPrioridad);
        Assert.Contains("Sin eventos", item.ExplicacionScore);
    }

    [Fact]
    public async Task ObtenerVistaScoreAsync_VencidoConTresExtraordinarios_AsignaPrioridadCritica()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-CRITICO", "Equipo crítico");
        SeedTresConfiguraciones(context, 1, requiereControl: false);
        SeedConfiguracion(context, 1, 1, requiereControl: true);
        SeedEvento(context, 1, 1, DateTime.Today.AddMonths(-3), DateTime.Today.AddDays(-20), extraordinario: true);
        SeedEvento(context, 1, 1, DateTime.Today.AddMonths(-2), DateTime.Today.AddDays(-10), extraordinario: true);
        SeedEvento(context, 1, 1, DateTime.Today.AddMonths(-1), DateTime.Today.AddDays(-1), extraordinario: true);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerVistaScoreAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30,
            TipoEventoMetrologicoId = 1
        });

        var item = Assert.Single(vista.Items);
        Assert.Equal(3, item.CantidadEventosExtraordinarios);
        Assert.Equal(90, item.ScoreMetrologico);
        Assert.Equal("Crítico", item.NivelPrioridad);
        Assert.Equal(1, vista.ResumenScore.ControlesCriticos);
    }

    private static ControlMetrologicoService CrearServicio(AppDbContext context) => new(context);

    private static ControlMetrologicoFiltroViewModel NuevosFiltros() => new() { HorizonteDias = 30 };

    private static void SeedCatalogos(AppDbContext context)
    {
        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" });
        context.SubtiposEvento.Add(new SubtipoEvento
        {
            SubtipoEventoId = 1,
            Nombre = "Periódico",
            Activo = true
        });
        context.TiposEventoMetrologico.AddRange(
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 1, Nombre = "Calibración" },
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 2, Nombre = "Verificación" },
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 3, Nombre = "Mantenimiento" });
    }

    private static void SeedEquipo(
        AppDbContext context,
        int id,
        string codigo,
        string nombre,
        int tipoEquipoId = 1,
        bool activo = true)
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
            Activo = activo
        });
    }

    private static void SeedTresConfiguraciones(
        AppDbContext context,
        int equipoId,
        bool requiereControl = true)
    {
        for (var tipoEventoId = 1; tipoEventoId <= 3; tipoEventoId++)
        {
            SeedConfiguracion(context, equipoId, tipoEventoId, requiereControl);
        }
    }

    private static void SeedConfiguracion(
        AppDbContext context,
        int equipoId,
        int tipoEventoId,
        bool requiereControl)
    {
        var existente = context.ConfiguracionesControlEquipo.Local.FirstOrDefault(c =>
            c.EquipoId == equipoId && c.TipoEventoMetrologicoId == tipoEventoId);

        if (existente != null)
        {
            existente.RequiereControl = requiereControl;
            return;
        }

        context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
        {
            EquipoId = equipoId,
            TipoEventoMetrologicoId = tipoEventoId,
            PeriodicidadValor = 4,
            PeriodicidadUnidad = "meses",
            RequiereControl = requiereControl,
            Activo = true
        });
    }

    private static void SeedEvento(
        AppDbContext context,
        int equipoId,
        int tipoEventoId,
        DateTime fechaEvento,
        DateTime? fechaProxima,
        bool extraordinario = false)
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
            Activo = true
        });
    }
}
