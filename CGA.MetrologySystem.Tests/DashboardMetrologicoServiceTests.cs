using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.DashboardMetrologico;
using CGA.MetrologySystem.Services.ControlMetrologico;
using CGA.MetrologySystem.Services.DashboardMetrologico;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class DashboardMetrologicoServiceTests
{
    [Fact]
    public async Task ObtenerDashboardAsync_SinEquipos_GeneraEstructuraCompletaYVacia()
    {
        await using var context = TestDbContextFactory.Create();
        var filtros = new DashboardMetrologicoFiltroViewModel { HorizonteDias = 0 };

        var dashboard = await CrearServicio(context).ObtenerDashboardAsync(filtros);

        Assert.Equal(30, dashboard.Filtros.HorizonteDias);
        Assert.Equal(0, dashboard.Resumen.TotalEquipos);
        Assert.Equal(0, dashboard.Resumen.PorcentajeCumplimiento);
        Assert.Equal(6, dashboard.DistribucionEstados.Count);
        Assert.Equal(6, dashboard.VencimientosPorMes.Count);
        Assert.Empty(dashboard.TopScore);
        Assert.Empty(dashboard.ControlesCriticos);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_EquipoConTresControlesVigentes_CalculaCumplimientoTotal()
    {
        await using var context = TestDbContextFactory.Create();
        SeedBase(context, "EQ-VIGENTE", "Equipo vigente");
        SeedTresControles(context, DateTime.Today.AddDays(90));
        await context.SaveChangesAsync();

        var dashboard = await CrearServicio(context).ObtenerDashboardAsync(NuevosFiltros());

        Assert.Equal(1, dashboard.Resumen.TotalEquipos);
        Assert.Equal(3, dashboard.Resumen.TotalControlesEvaluados);
        Assert.Equal(1, dashboard.Resumen.EquiposVigentes);
        Assert.Equal(100, dashboard.Resumen.PorcentajeCumplimiento);
        Assert.Equal(3, dashboard.TopScore.Count);
        Assert.Equal(1, dashboard.DistribucionEstados.Single(e => e.Estado == "Vigente").Cantidad);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_EstadosMixtos_AgrupaControlesPorTipo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedBase(context, "EQ-MIXTO", "Equipo mixto");
        SeedEvento(context, 1, DateTime.Today.AddDays(-2));
        SeedEvento(context, 2, DateTime.Today.AddDays(10));
        SeedEvento(context, 3, DateTime.Today.AddDays(90));
        await context.SaveChangesAsync();

        var dashboard = await CrearServicio(context).ObtenerDashboardAsync(NuevosFiltros());

        Assert.Equal(1, dashboard.Resumen.EquiposVencidos);
        Assert.Equal(1, dashboard.Resumen.ControlesVencidos);
        Assert.Equal(1, dashboard.Resumen.ControlesProximosAVencer);
        Assert.Equal(1, dashboard.ControlesPorTipo.Single(t => t.TipoControl == "Calibración").Vencidos);
        Assert.Equal(1, dashboard.ControlesPorTipo.Single(t => t.TipoControl == "Verificación").ProximosAVencer);
        Assert.Equal(1, dashboard.ControlesPorTipo.Single(t => t.TipoControl == "Mantenimiento").Vigentes);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_SinScoresCriticos_MuestraControlesDePrioridadAltaComoAlternativa()
    {
        await using var context = TestDbContextFactory.Create();
        SeedBase(context, "EQ-ALTO", "Equipo prioridad alta");
        SeedEvento(context, 1, DateTime.Today.AddDays(-1));
        SeedEvento(context, 2, DateTime.Today.AddDays(90));
        SeedEvento(context, 3, DateTime.Today.AddDays(90));
        await context.SaveChangesAsync();

        var dashboard = await CrearServicio(context).ObtenerDashboardAsync(NuevosFiltros());

        Assert.Equal(0, dashboard.Resumen.ControlesCriticosScore);
        var control = Assert.Single(dashboard.ControlesCriticos);
        Assert.Equal("EQ-ALTO", control.CodigoEquipo);
        Assert.Equal("Alto", control.NivelPrioridad);
        Assert.Equal(70, control.ScoreMetrologico);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_VencimientosFuturos_LosAgrupaEnMesCorrespondiente()
    {
        await using var context = TestDbContextFactory.Create();
        SeedBase(context, "EQ-MES", "Equipo mensual");
        var inicioMesSiguiente = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        SeedTresControles(context, inicioMesSiguiente.AddDays(5));
        await context.SaveChangesAsync();

        var dashboard = await CrearServicio(context).ObtenerDashboardAsync(new DashboardMetrologicoFiltroViewModel
        {
            HorizonteDias = 90
        });

        var mes = dashboard.VencimientosPorMes.Single(m => m.Mes == inicioMesSiguiente.ToString("yyyy-MM"));
        Assert.Equal(3, mes.Cantidad);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_BusquedaSinCoincidencias_RetornaDashboardVacio()
    {
        await using var context = TestDbContextFactory.Create();
        SeedBase(context, "EQ-REAL", "Equipo existente");
        SeedTresControles(context, DateTime.Today.AddDays(90));
        await context.SaveChangesAsync();

        var dashboard = await CrearServicio(context).ObtenerDashboardAsync(new DashboardMetrologicoFiltroViewModel
        {
            HorizonteDias = 30,
            Buscar = "NO-EXISTE"
        });

        Assert.Equal(0, dashboard.Resumen.TotalEquipos);
        Assert.Equal(0, dashboard.Resumen.TotalControlesEvaluados);
        Assert.Empty(dashboard.TopScore);
    }

    private static DashboardMetrologicoService CrearServicio(AppDbContext context)
    {
        return new DashboardMetrologicoService(new ControlMetrologicoService(context));
    }

    private static DashboardMetrologicoFiltroViewModel NuevosFiltros() => new() { HorizonteDias = 30 };

    private static void SeedBase(AppDbContext context, string codigo, string nombre)
    {
        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" });
        context.Equipos.Add(new Equipo
        {
            EquipoId = 1,
            Codigo = codigo,
            Nombre = nombre,
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        });
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
        for (var tipoEventoId = 1; tipoEventoId <= 3; tipoEventoId++)
        {
            context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
            {
                EquipoId = 1,
                TipoEventoMetrologicoId = tipoEventoId,
                PeriodicidadValor = 4,
                PeriodicidadUnidad = "meses",
                RequiereControl = true,
                Activo = true
            });
        }
    }

    private static void SeedTresControles(AppDbContext context, DateTime fechaProxima)
    {
        for (var tipoEventoId = 1; tipoEventoId <= 3; tipoEventoId++)
        {
            SeedEvento(context, tipoEventoId, fechaProxima);
        }
    }

    private static void SeedEvento(AppDbContext context, int tipoEventoId, DateTime fechaProxima)
    {
        context.EventosMetrologicos.Add(new EventoMetrologico
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = tipoEventoId,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = DateTime.Today.AddMonths(-1),
            FechaProxima = fechaProxima.Date,
            Activo = true
        });
    }
}
