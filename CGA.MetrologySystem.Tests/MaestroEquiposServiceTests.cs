using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Models.MaestroEquipos;
using CGA.MetrologySystem.Services.ControlMetrologico;
using CGA.MetrologySystem.Services.MaestroEquipos;
using ClosedXML.Excel;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class MaestroEquiposServiceTests
{
    [Fact]
    public async Task ObtenerIndexAsync_SinEquipos_RetornaVistaVaciaYNormalizaHorizonte()
    {
        await using var context = TestDbContextFactory.Create();
        var filtros = new MaestroEquiposFiltroViewModel { HorizonteDias = -1 };

        var vista = await CrearServicio(context).ObtenerIndexAsync(filtros);

        Assert.Empty(vista.Equipos);
        Assert.Equal(0, vista.TotalEquipos);
        Assert.Equal(30, vista.Filtros.HorizonteDias);
        Assert.Equal(Enum.GetValues<EstadoControlMetrologico>().Length, vista.Filtros.EstadosGlobales.Count);
    }

    [Fact]
    public async Task ObtenerIndexAsync_EquipoSinConfiguracion_MarcaTresControlesIncompletos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-INCOMPLETO", "Equipo incompleto");
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(NuevosFiltros());

        var equipo = Assert.Single(vista.Equipos);
        Assert.True(equipo.TieneConfiguracionIncompleta);
        Assert.All(equipo.Controles, c => Assert.True(c.RequiereConfiguracion));
        Assert.All(equipo.Controles, c => Assert.Null(c.ConfiguracionControlEquipoId));
        Assert.Equal(1, vista.TotalConConfiguracionIncompleta);
    }

    [Fact]
    public async Task ObtenerIndexAsync_ConfiguracionesCompletas_MapeaIdsYScoreMaximo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-COMPLETO", "Equipo completo");
        SeedTresConfiguraciones(context, 1, requiereControl: false);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(NuevosFiltros());

        var equipo = Assert.Single(vista.Equipos);
        Assert.False(equipo.TieneConfiguracionIncompleta);
        Assert.All(equipo.Controles, c => Assert.True(c.ConfiguracionControlEquipoId.HasValue));
        Assert.All(equipo.Controles, c => Assert.True(c.NoRequiereControl));
        Assert.Equal(0, equipo.ScoreMaximo);
        Assert.Equal("Bajo", equipo.PrioridadMaxima);
    }

    [Fact]
    public async Task ObtenerIndexAsync_SoloConfiguracionIncompleta_ExcluyeEquiposCompletos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-INCOMPLETO", "Sin configuración");
        SeedEquipo(context, 2, "EQ-COMPLETO", "Configurado");
        SeedTresConfiguraciones(context, 2, requiereControl: false);
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new MaestroEquiposFiltroViewModel
        {
            HorizonteDias = 30,
            SoloConfiguracionIncompleta = true
        });

        Assert.Equal("EQ-INCOMPLETO", Assert.Single(vista.Equipos).CodigoEquipo);
        Assert.Equal(1, vista.TotalConConfiguracionIncompleta);
    }

    [Fact]
    public async Task ObtenerIndexAsync_FiltroEstadoGlobal_DevuelveSoloEquipoVencido()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-VENCIDO", "Equipo vencido");
        SeedEquipo(context, 2, "EQ-VIGENTE", "Equipo vigente");
        SeedTresConfiguraciones(context, 1);
        SeedTresConfiguraciones(context, 2);
        SeedTresEventos(context, 1, DateTime.Today.AddDays(-1));
        SeedTresEventos(context, 2, DateTime.Today.AddDays(90));
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(new MaestroEquiposFiltroViewModel
        {
            HorizonteDias = 30,
            EstadoGlobal = EstadoControlMetrologico.Vencido
        });

        var equipo = Assert.Single(vista.Equipos);
        Assert.Equal("EQ-VENCIDO", equipo.CodigoEquipo);
        Assert.Equal(1, vista.TotalVencidos);
        Assert.Contains(vista.Filtros.EstadosGlobales, e =>
            e.Value == EstadoControlMetrologico.Vencido.ToString() && e.Selected);
    }

    [Fact]
    public async Task ObtenerIndexAsync_OrdenaConfiguracionIncompletaAntesQueEstadoVencido()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "ZZZ-INCOMPLETO", "Equipo incompleto");
        SeedEquipo(context, 2, "AAA-VENCIDO", "Equipo vencido");
        SeedTresConfiguraciones(context, 2);
        SeedTresEventos(context, 2, DateTime.Today.AddDays(-1));
        await context.SaveChangesAsync();

        var vista = await CrearServicio(context).ObtenerIndexAsync(NuevosFiltros());

        Assert.Equal("ZZZ-INCOMPLETO", vista.Equipos[0].CodigoEquipo);
        Assert.Equal("AAA-VENCIDO", vista.Equipos[1].CodigoEquipo);
    }

    [Fact]
    public async Task GenerarListadoMaestroAsync_CreaExcelConEncabezadosDatosYMetadatos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogos(context);
        SeedEquipo(context, 1, "EQ-EXCEL", "Equipo exportado");
        SeedTresConfiguraciones(context, 1);
        SeedTresEventos(context, 1, DateTime.Today.AddDays(45));
        await context.SaveChangesAsync();
        var excelService = new MaestroEquiposExcelService(CrearServicio(context));

        var resultado = await excelService.GenerarListadoMaestroAsync(
            NuevosFiltros(),
            "Usuario Pruebas");

        Assert.NotEmpty(resultado.Contenido);
        Assert.StartsWith("ListadoMaestroEquipos_", resultado.NombreArchivo);
        Assert.EndsWith(".xlsx", resultado.NombreArchivo);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            resultado.ContentType);

        using var stream = new MemoryStream(resultado.Contenido);
        using var workbook = new XLWorkbook(stream);
        var hoja = workbook.Worksheet("Listado Maestro");
        Assert.Equal("Codigo equipo", hoja.Cell(1, 1).GetString());
        Assert.Equal("EQ-EXCEL", hoja.Cell(2, 1).GetString());
        Assert.Equal("Equipo exportado", hoja.Cell(2, 2).GetString());
        Assert.Equal("Usuario Pruebas", hoja.Cell(2, 24).GetString());
        Assert.Contains("Horizonte dias: 30", hoja.Cell(2, 25).GetString());
    }

    private static MaestroEquiposService CrearServicio(AppDbContext context)
    {
        return new MaestroEquiposService(context, new ControlMetrologicoService(context));
    }

    private static MaestroEquiposFiltroViewModel NuevosFiltros() => new() { HorizonteDias = 30 };

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

    private static void SeedEquipo(AppDbContext context, int id, string codigo, string nombre)
    {
        context.Equipos.Add(new Equipo
        {
            EquipoId = id,
            Codigo = codigo,
            Nombre = nombre,
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        });
    }

    private static void SeedTresConfiguraciones(
        AppDbContext context,
        int equipoId,
        bool requiereControl = true)
    {
        for (var tipoEventoId = 1; tipoEventoId <= 3; tipoEventoId++)
        {
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
    }

    private static void SeedTresEventos(AppDbContext context, int equipoId, DateTime fechaProxima)
    {
        for (var tipoEventoId = 1; tipoEventoId <= 3; tipoEventoId++)
        {
            context.EventosMetrologicos.Add(new EventoMetrologico
            {
                EquipoId = equipoId,
                TipoEventoMetrologicoId = tipoEventoId,
                SubtipoEventoId = 1,
                ResponsableInternoId = 1,
                FechaEvento = DateTime.Today.AddMonths(-1),
                FechaProxima = fechaProxima.Date,
                Activo = true
            });
        }
    }
}
