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

    [Theory]
    [InlineData("dias", 10, 2026, 1, 23)]
    [InlineData("días", 10, 2026, 1, 23)]
    [InlineData("anios", 2, 2028, 1, 13)]
    [InlineData("años", 2, 2028, 1, 13)]
    [InlineData(" MESES ", 2, 2026, 3, 13)]
    public async Task CalcularProximaFechaAsync_AceptaUnidadesSoportadas(
        string unidad,
        int periodicidad,
        int anioEsperado,
        int mesEsperado,
        int diaEsperado)
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, periodicidad, unidad);
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.CalcularProximaFechaAsync(
            1,
            2,
            new DateTime(2026, 1, 13, 18, 30, 0));

        Assert.Equal(new DateTime(anioEsperado, mesEsperado, diaEsperado), resultado);
    }

    [Theory]
    [InlineData(null, "meses")]
    [InlineData(0, "meses")]
    [InlineData(-1, "meses")]
    [InlineData(4, null)]
    [InlineData(4, "  ")]
    public async Task CalcularProximaFechaAsync_PeriodicidadInvalida_RetornaNull(
        int? periodicidad,
        string? unidad)
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            PeriodicidadValor = periodicidad,
            PeriodicidadUnidad = unidad,
            RequiereControl = true,
            Activo = true
        });
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.CalcularProximaFechaAsync(1, 2, DateTime.Today);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task CalcularProximaFechaAsync_ConfiguracionInactiva_RetornaNull()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, 4, "meses", activo: false);
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        Assert.Null(await service.CalcularProximaFechaAsync(1, 2, DateTime.Today));
    }

    [Fact]
    public async Task CalcularProximaFechaAsync_EquipoQueNoRequiereControl_RetornaNull()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, 4, "meses", requiereControl: false);
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        Assert.Null(await service.CalcularProximaFechaAsync(1, 2, DateTime.Today));
    }

    [Fact]
    public async Task CalcularProximaFechaAsync_UnidadNoSoportada_LanzaExcepcion()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, 1, "semanas");
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalcularProximaFechaAsync(1, 2, DateTime.Today));
        Assert.Contains("semanas", excepcion.Message);
    }

    [Fact]
    public async Task EvaluarEventoAsync_SinRequerirControl_PermiteRegistro()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, null, null, requiereControl: false, permitePorIngreso: true);
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(1, 2, DateTime.Today, 1, null);

        Assert.True(resultado.EsValido);
        Assert.True(resultado.TieneConfiguracion);
        Assert.False(resultado.RequiereControl);
        Assert.True(resultado.PermitePorIngreso);
        Assert.Contains("no requiere control", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluarEventoAsync_SinRequerirControlPeroExtraordinarioSinJustificacion_RechazaRegistro()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, null, null, requiereControl: false);
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(1, 2, DateTime.Today, 2, "  ");

        Assert.False(resultado.EsValido);
        Assert.True(resultado.EsExtraordinario);
        Assert.Contains("justificación", resultado.Mensaje);
    }

    [Fact]
    public async Task EvaluarEventoAsync_PeriodicidadInvalida_RechazaRegistro()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracion(context, 0, "meses");
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(1, 2, DateTime.Today, 1, null);

        Assert.False(resultado.EsValido);
        Assert.Contains("periodicidad válida", resultado.Mensaje);
        Assert.Null(resultado.FechaProximaCalculada);
    }

    [Fact]
    public async Task EvaluarEventoAsync_DesviacionEnLimiteDe20Dias_PermiteEventoPeriodico()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracionVerificacion(context);
        context.EventosMetrologicos.Add(CrearEvento(new DateTime(2026, 1, 1)));
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            1, 2, new DateTime(2026, 5, 21), 1, null);

        Assert.True(resultado.EsValido);
        Assert.False(resultado.EsExtraordinario);
        Assert.Equal(20, resultado.DiasDesviacion);
        Assert.Equal("Evento evaluado correctamente.", resultado.Mensaje);
    }

    [Fact]
    public async Task EvaluarEventoAsync_ExtraordinarioExplicitoSinJustificacion_RechazaRegistro()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracionVerificacion(context);
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            1, 2, new DateTime(2026, 2, 1), 2, null);

        Assert.False(resultado.EsValido);
        Assert.True(resultado.EsExtraordinario);
        Assert.Contains("justificación", resultado.Mensaje);
        Assert.NotNull(resultado.Advertencia);
    }

    [Fact]
    public async Task EvaluarEventoAsync_EventoHistoricoIgnoraDesviacionMayorA20Dias()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracionVerificacion(context);
        context.EventosMetrologicos.Add(CrearEvento(new DateTime(2025, 1, 1)));
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            1, 2, new DateTime(2026, 1, 1), 1, null, esHistorico: true);

        Assert.True(resultado.EsValido);
        Assert.False(resultado.EsExtraordinario);
        Assert.Null(resultado.DiasDesviacion);
        Assert.Equal(new DateTime(2025, 5, 1), resultado.FechaEsperada);
    }

    [Fact]
    public async Task EvaluarEventoAsync_UsaElEventoAnteriorMasReciente()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosMinimos(context);
        SeedConfiguracionVerificacion(context);
        context.EventosMetrologicos.AddRange(
            CrearEvento(new DateTime(2025, 1, 1)),
            CrearEvento(new DateTime(2026, 1, 1)),
            CrearEvento(new DateTime(2027, 1, 1)));
        await context.SaveChangesAsync();

        var service = new MetrologyRulesService(context);

        var resultado = await service.EvaluarEventoAsync(
            1, 2, new DateTime(2026, 5, 1), 1, null);

        Assert.True(resultado.EsValido);
        Assert.Equal(new DateTime(2026, 1, 1), resultado.FechaUltimoEvento);
        Assert.Equal(new DateTime(2026, 5, 1), resultado.FechaEsperada);
        Assert.Equal(0, resultado.DiasDesviacion);
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
        SeedConfiguracion(context, 4, "meses");
    }

    private static void SeedConfiguracion(
        Infrastructure.Persistence.AppDbContext context,
        int? periodicidad,
        string? unidad,
        bool requiereControl = true,
        bool permitePorIngreso = false,
        bool activo = true)
    {
        context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            PeriodicidadValor = periodicidad,
            PeriodicidadUnidad = unidad,
            RequiereControl = requiereControl,
            PermitePorIngreso = permitePorIngreso,
            Activo = activo
        });
    }

    private static EventoMetrologico CrearEvento(DateTime fechaEvento)
    {
        return new EventoMetrologico
        {
            EquipoId = 1,
            TipoEventoMetrologicoId = 2,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = fechaEvento,
            Activo = true
        };
    }
}
