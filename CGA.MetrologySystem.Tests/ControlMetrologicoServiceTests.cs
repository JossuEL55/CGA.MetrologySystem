using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Services.ControlMetrologico;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class ControlMetrologicoServiceTests
{
    [Fact]
    public async Task ObtenerVistaPorEquipoAsync_ClasificaControlesVigenteProximoYVencido()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEquipoConTresControles(context);
        await context.SaveChangesAsync();

        var service = new ControlMetrologicoService(context);

        var vista = await service.ObtenerVistaPorEquipoAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30
        });

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal(EstadoControlMetrologico.Vigente, equipo.Calibracion.Estado);
        Assert.Equal(EstadoControlMetrologico.ProximoAVencer, equipo.Verificacion.Estado);
        Assert.Equal(EstadoControlMetrologico.Vencido, equipo.Mantenimiento.Estado);
        Assert.Equal(EstadoControlMetrologico.Vencido, equipo.EstadoGlobal);
        Assert.Equal(1, vista.Resumen.Vencidos);
    }

    [Fact]
    public async Task ObtenerVistaScoreAsync_AsignaPrioridadAltaACalibracionVencida()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEquipoConTresControles(context);

        var calibracion = context.EventosMetrologicos.Local
            .Single(e => e.TipoEventoMetrologicoId == 1);
        calibracion.FechaProxima = DateTime.Today.AddDays(-2);
        calibracion.EsExtraordinario = true;

        await context.SaveChangesAsync();

        var service = new ControlMetrologicoService(context);

        var vista = await service.ObtenerVistaScoreAsync(new ControlMetrologicoFiltroViewModel
        {
            HorizonteDias = 30
        });

        var itemCalibracion = vista.Items.Single(i => i.TipoEventoNombre == "Calibración");
        Assert.Equal(EstadoControlMetrologico.Vencido, itemCalibracion.Estado);
        Assert.True(itemCalibracion.ScoreMetrologico >= 70);
        Assert.Equal("Alto", itemCalibracion.NivelPrioridad);
    }

    private static void SeedEquipoConTresControles(AppDbContext context)
    {
        context.TiposEquipo.Add(new TipoEquipo
        {
            TipoEquipoId = 1,
            Nombre = "Patrón de prueba"
        });

        context.Equipos.Add(new Equipo
        {
            EquipoId = 1,
            Codigo = "CGA-CTRL-001",
            Nombre = "Equipo controlado",
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

        context.ConfiguracionesControlEquipo.AddRange(
            CrearConfiguracion(1, 1),
            CrearConfiguracion(1, 2),
            CrearConfiguracion(1, 3));

        context.EventosMetrologicos.AddRange(
            CrearEvento(1, DateTime.Today.AddMonths(-1), DateTime.Today.AddDays(90)),
            CrearEvento(2, DateTime.Today.AddMonths(-3), DateTime.Today.AddDays(15)),
            CrearEvento(3, DateTime.Today.AddMonths(-5), DateTime.Today.AddDays(-7)));
    }

    private static ConfiguracionControlEquipo CrearConfiguracion(int equipoId, int tipoEventoId)
    {
        return new ConfiguracionControlEquipo
        {
            EquipoId = equipoId,
            TipoEventoMetrologicoId = tipoEventoId,
            PeriodicidadValor = 4,
            PeriodicidadUnidad = "meses",
            RequiereControl = true,
            Activo = true
        };
    }

    private static EventoMetrologico CrearEvento(
        int tipoEventoId,
        DateTime fechaEvento,
        DateTime fechaProxima)
    {
        return new EventoMetrologico
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = tipoEventoId,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = fechaEvento.Date,
            FechaProxima = fechaProxima.Date,
            Activo = true
        };
    }
}
