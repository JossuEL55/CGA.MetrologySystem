using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Services;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class MetrologyRulesServiceTests
{
    [Fact]
    public async Task CalcularProximaFechaAsync_UsaPeriodicidadConfiguradaEnMeses()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            PeriodicidadValor = 4,
            PeriodicidadUnidad = "meses",
            RequiereControl = true,
            Activo = true
        });
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var proximaFecha = await service.CalcularProximaFechaAsync(
            equipoId: 1,
            tipoEventoMetrologicoId: 2,
            fechaEvento: new DateTime(2026, 1, 13));

        Assert.Equal(new DateTime(2026, 5, 13), proximaFecha);
    }

    [Fact]
    public async Task EvaluarEventoAsync_SinConfiguracionActiva_RechazaRegistro()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            equipoId: 1,
            tipoEventoMetrologicoId: 2,
            fechaEvento: DateTime.Today,
            subtipoEventoId: 1,
            justificacionExtraordinario: null);

        Assert.False(resultado.EsValido);
        Assert.False(resultado.TieneConfiguracion);
        Assert.Equal("El equipo no tiene una configuración activa para este tipo de evento.", resultado.Mensaje);
    }

    [Fact]
    public async Task EvaluarEventoAsync_DesviacionMayorA20Dias_ExigeSubtipoExtraordinario()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracionVerificacion(context);
        context.EventosMetrologicos.Add(new EventoMetrologico
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = new DateTime(2026, 1, 13),
            FechaProxima = new DateTime(2026, 5, 13),
            Activo = true
        });
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            equipoId: 1,
            tipoEventoMetrologicoId: 2,
            fechaEvento: new DateTime(2026, 6, 20),
            subtipoEventoId: 1,
            justificacionExtraordinario: null);

        Assert.False(resultado.EsValido);
        Assert.True(resultado.EsExtraordinario);
        Assert.True(resultado.DiasDesviacion > 20);
        Assert.Contains("Extraordinario", resultado.Mensaje);
    }

    [Fact]
    public async Task EvaluarEventoAsync_ExtraordinarioJustificado_PermiteRegistroYCalculaProximaFecha()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracionVerificacion(context);
        context.EventosMetrologicos.Add(new EventoMetrologico
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = new DateTime(2026, 1, 13),
            FechaProxima = new DateTime(2026, 5, 13),
            Activo = true
        });
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            equipoId: 1,
            tipoEventoMetrologicoId: 2,
            fechaEvento: new DateTime(2026, 6, 20),
            subtipoEventoId: 2,
            justificacionExtraordinario: "Control fuera de planificación por condición operativa.");

        Assert.True(resultado.EsValido);
        Assert.True(resultado.EsExtraordinario);
        Assert.Equal(new DateTime(2026, 10, 20), resultado.FechaProximaCalculada);
    }

    private static void SeedCatalogosMinimos(Infrastructure.Persistence.AppDbContext context)
    {
        context.Equipos.Add(new Equipo
        {
            EquipoId = 1,
            Codigo = "CGA-TEST-001",
            Nombre = "Equipo de prueba",
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        });

        context.SubtiposEvento.AddRange(
            new SubtipoEvento { SubtipoEventoId = 1, Nombre = "Periódico", Activo = true },
            new SubtipoEvento { SubtipoEventoId = 2, Nombre = "Extraordinario", Activo = true });

        context.TiposEventoMetrologico.Add(new TipoEventoMetrologico
        {
            TipoEventoMetrologicoId = 2,
            Nombre = "Verificación"
        });
    }

    private static void SeedConfiguracionVerificacion(Infrastructure.Persistence.AppDbContext context)
    {
        context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            PeriodicidadValor = 4,
            PeriodicidadUnidad = "meses",
            RequiereControl = true,
            Activo = true
        });
    }
}
