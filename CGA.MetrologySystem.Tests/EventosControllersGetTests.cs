using CGA.MetrologySystem.Controllers;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class EventosControllersGetTests
{
    [Fact]
    public async Task CalibracionesIndex_ConTodosLosFiltros_RetornaListaFiltrada()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosYEventos(context);
        await context.SaveChangesAsync();
        var controller = CrearCalibraciones(context);

        var result = await controller.Index(
            "EQ-FILTRO",
            "Operativo",
            1,
            1,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        var items = Assert.IsType<List<EventoCalibracionDato>>(Assert.IsType<ViewResult>(result).Model);
        Assert.Single(items);
        Assert.Equal("EQ-FILTRO", controller.ViewBag.Buscar);
        Assert.NotNull(controller.ViewBag.Laboratorios);
        Assert.NotNull(controller.ViewBag.Estados);
    }

    [Theory]
    [InlineData("historicas")]
    [InlineData("extraordinarias")]
    [InlineData("operativas")]
    [InlineData("todas")]
    public async Task VerificacionesIndex_ClasificacionesSoportadas_RetornanVista(string clasificacion)
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosYEventos(context);
        await context.SaveChangesAsync();
        var controller = CrearVerificaciones(context);

        var result = await controller.Index(
            "EQ-FILTRO",
            "Operativo",
            1,
            clasificacion,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.IsType<List<EventoVerificacionDato>>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotNull(controller.ViewBag.Clasificaciones);
    }

    [Theory]
    [InlineData("historicos")]
    [InlineData("extraordinarios")]
    [InlineData("operativos")]
    [InlineData("todos")]
    public async Task MantenimientosIndex_ClasificacionesSoportadas_RetornanVista(string clasificacion)
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosYEventos(context);
        await context.SaveChangesAsync();
        var controller = CrearMantenimientos(context);

        var result = await controller.Index(
            "EQ-FILTRO",
            "Operativo",
            1,
            1,
            clasificacion,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.IsType<List<EventoMantenimientoDato>>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotNull(controller.ViewBag.TiposMantenimiento);
        Assert.NotNull(controller.ViewBag.Clasificaciones);
    }

    [Fact]
    public async Task Details_IdsNulos_RetornanNotFoundEnLosTresControladores()
    {
        await using var context = TestDbContextFactory.Create();

        Assert.IsType<NotFoundResult>(await CrearCalibraciones(context).Details(null));
        Assert.IsType<NotFoundResult>(await CrearVerificaciones(context).Details(null));
        Assert.IsType<NotFoundResult>(await CrearMantenimientos(context).Details(null));
    }

    [Fact]
    public async Task Details_RegistrosInexistentes_RetornanNotFoundEnLosTresControladores()
    {
        await using var context = TestDbContextFactory.Create();

        Assert.IsType<NotFoundResult>(await CrearCalibraciones(context).Details(999));
        Assert.IsType<NotFoundResult>(await CrearVerificaciones(context).Details(999));
        Assert.IsType<NotFoundResult>(await CrearMantenimientos(context).Details(999));
    }

    [Fact]
    public async Task Details_RegistrosExistentes_RetornanVistasConRelaciones()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosYEventos(context);
        await context.SaveChangesAsync();

        var calibracion = Assert.IsType<EventoCalibracionDato>(
            Assert.IsType<ViewResult>(await CrearCalibraciones(context).Details(1)).Model);
        var verificacion = Assert.IsType<EventoVerificacionDato>(
            Assert.IsType<ViewResult>(await CrearVerificaciones(context).Details(1)).Model);
        var mantenimiento = Assert.IsType<EventoMantenimientoDato>(
            Assert.IsType<ViewResult>(await CrearMantenimientos(context).Details(1)).Model);

        Assert.Equal("EQ-FILTRO", calibracion.EventoMetrologico.Equipo.Codigo);
        Assert.Single(verificacion.EventoMetrologico.ResultadosVerificacion);
        Assert.Single(mantenimiento.EventoMetrologico.ActividadesMantenimiento);
    }

    [Fact]
    public async Task CreateGet_Calibracion_InicializaFechasYCombos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosBasicos(context);
        await context.SaveChangesAsync();

        var result = await CrearCalibraciones(context).Create();

        var model = Assert.IsType<CalibracionViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(DateTime.Today, model.FechaEvento);
        Assert.Equal(DateTime.Today, model.FechaCalibracion);
        Assert.NotEmpty(model.Equipos);
    }

    [Fact]
    public async Task CreateGet_VerificacionSinPlantilla_AgregaResultadoInicial()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosBasicos(context);
        await context.SaveChangesAsync();

        var result = await CrearVerificaciones(context).Create(equipoId: 1);

        var model = Assert.IsType<VerificacionViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(1, model.EquipoId);
        var item = Assert.Single(model.Resultados);
        Assert.Equal(1, item.Orden);
        Assert.True(item.Cumple);
    }

    [Fact]
    public async Task CreateGet_MantenimientoSinPlantilla_AgregaActividadInicial()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosBasicos(context);
        await context.SaveChangesAsync();

        var result = await CrearMantenimientos(context).Create(equipoId: 1);

        var model = Assert.IsType<MantenimientoViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(1, model.EquipoId);
        Assert.Equal(1, Assert.Single(model.Actividades).Orden);
    }

    [Fact]
    public async Task EditYDelete_IdsNulos_RetornanNotFound()
    {
        await using var context = TestDbContextFactory.Create();

        Assert.IsType<NotFoundResult>(await CrearCalibraciones(context).Edit(null));
        Assert.IsType<NotFoundResult>(await CrearCalibraciones(context).Delete(null));
        Assert.IsType<NotFoundResult>(await CrearVerificaciones(context).Edit(null));
        Assert.IsType<NotFoundResult>(await CrearVerificaciones(context).Delete(null));
        Assert.IsType<NotFoundResult>(await CrearMantenimientos(context).Edit(null));
        Assert.IsType<NotFoundResult>(await CrearMantenimientos(context).Delete(null));
    }

    [Fact]
    public async Task EditGet_RegistrosExistentes_MapeaModelosYRelaciones()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosYEventos(context);
        await context.SaveChangesAsync();

        var calibracion = Assert.IsType<CalibracionViewModel>(
            Assert.IsType<ViewResult>(await CrearCalibraciones(context).Edit(1)).Model);
        var verificacion = Assert.IsType<VerificacionViewModel>(
            Assert.IsType<ViewResult>(await CrearVerificaciones(context).Edit(1)).Model);
        var mantenimiento = Assert.IsType<MantenimientoViewModel>(
            Assert.IsType<ViewResult>(await CrearMantenimientos(context).Edit(1)).Model);

        Assert.Equal(1, calibracion.EquipoId);
        Assert.Equal("CERT-FILTRO", calibracion.NumeroCertificado);
        Assert.Equal(1, verificacion.EquipoId);
        Assert.Single(verificacion.Resultados);
        Assert.Equal(1, mantenimiento.EquipoId);
        Assert.Single(mantenimiento.Actividades);
    }

    [Fact]
    public async Task DeleteGet_RegistrosExistentes_RetornaEntidadesParaConfirmacion()
    {
        await using var context = TestDbContextFactory.Create();
        SeedCatalogosYEventos(context);
        await context.SaveChangesAsync();

        var calibracion = Assert.IsType<EventoCalibracionDato>(
            Assert.IsType<ViewResult>(await CrearCalibraciones(context).Delete(1)).Model);
        var verificacion = Assert.IsType<EventoVerificacionDato>(
            Assert.IsType<ViewResult>(await CrearVerificaciones(context).Delete(1)).Model);
        var mantenimiento = Assert.IsType<EventoMantenimientoDato>(
            Assert.IsType<ViewResult>(await CrearMantenimientos(context).Delete(1)).Model);

        Assert.Equal(1, calibracion.EventoCalibracionDatoId);
        Assert.Equal(1, verificacion.EventoVerificacionDatoId);
        Assert.Equal(1, mantenimiento.EventoMantenimientoDatoId);
    }

    private static CalibracionesController CrearCalibraciones(AppDbContext context)
    {
        var controller = new CalibracionesController(
            context,
            new TestGoogleDriveService(),
            new TestMetrologyRulesService(),
            new TestNotificacionMetrologicaService(),
            new TestAuditoriaMetrologicaService(),
            new TestUserManager());
        ConfigurarController(controller);
        return controller;
    }

    private static VerificacionesController CrearVerificaciones(AppDbContext context)
    {
        var controller = new VerificacionesController(
            context,
            new VerificacionPdfService(),
            new TestGoogleDriveService(),
            new TestMetrologyRulesService(),
            new TestNotificacionMetrologicaService(),
            new TestAuditoriaMetrologicaService(),
            new TestUserManager(),
            NullLogger<VerificacionesController>.Instance);
        ConfigurarController(controller);
        return controller;
    }

    private static MantenimientosController CrearMantenimientos(AppDbContext context)
    {
        var controller = new MantenimientosController(
            context,
            new MantenimientoPdfService(),
            new TestGoogleDriveService(),
            new TestMetrologyRulesService(),
            new TestNotificacionMetrologicaService(),
            new TestAuditoriaMetrologicaService(),
            new TestUserManager());
        ConfigurarController(controller);
        return controller;
    }

    private static void ConfigurarController(Controller controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "administrador.pruebas"),
                    new Claim(ClaimTypes.Role, RolesSistema.AdministradorMetrologico)
                }, "Tests"))
            }
        };
        controller.Url = new TestUrlHelper();
    }

    private static void SeedCatalogosBasicos(AppDbContext context)
    {
        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" });
        context.Equipos.Add(new Equipo
        {
            EquipoId = 1,
            Codigo = "EQ-FILTRO",
            Nombre = "Equipo filtrable",
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        });
        context.ResponsablesInternos.Add(new ResponsableInterno
        {
            ResponsableInternoId = 1,
            NombreCompleto = "Responsable",
            Activo = true
        });
        context.SubtiposEvento.Add(new SubtipoEvento
        {
            SubtipoEventoId = 1,
            Nombre = "Periódico",
            Activo = true
        });
        context.TiposMantenimiento.Add(new TipoMantenimiento
        {
            TipoMantenimientoId = 1,
            Nombre = "Preventivo",
            Activo = true
        });
        context.Laboratorios.Add(new Laboratorio
        {
            LaboratorioId = 1,
            Nombre = "Laboratorio",
            Activo = true
        });
        context.TiposEventoMetrologico.AddRange(
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 1, Nombre = "Calibración" },
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 2, Nombre = "Verificación" },
            new TipoEventoMetrologico { TipoEventoMetrologicoId = 3, Nombre = "Mantenimiento" });
    }

    private static void SeedCatalogosYEventos(AppDbContext context)
    {
        SeedCatalogosBasicos(context);
        var equipo = context.Equipos.Local.Single();
        var responsable = context.ResponsablesInternos.Local.Single();
        var subtipo = context.SubtiposEvento.Local.Single();

        var eventoCalibracion = CrearEvento(equipo, responsable, subtipo, 1, false, false);
        var eventoVerificacion = CrearEvento(equipo, responsable, subtipo, 2, true, false);
        var eventoMantenimiento = CrearEvento(equipo, responsable, subtipo, 3, false, true);
        var calibracion = new EventoCalibracionDato
        {
            EventoCalibracionDatoId = 1,
            EventoMetrologico = eventoCalibracion,
            NumeroCertificado = "CERT-FILTRO",
            LaboratorioId = 1,
            Laboratorio = context.Laboratorios.Local.Single()
        };
        var verificacion = new EventoVerificacionDato
        {
            EventoVerificacionDatoId = 1,
            EventoMetrologico = eventoVerificacion
        };
        var mantenimiento = new EventoMantenimientoDato
        {
            EventoMantenimientoDatoId = 1,
            EventoMetrologico = eventoMantenimiento,
            TipoMantenimientoId = 1,
            TipoMantenimiento = context.TiposMantenimiento.Local.Single()
        };
        eventoVerificacion.ResultadosVerificacion.Add(new EventoVerificacionResultado
        {
            DescripcionItem = "Resultado",
            Cumple = true,
            Orden = 1,
            EventoMetrologico = eventoVerificacion
        });
        eventoMantenimiento.ActividadesMantenimiento.Add(new EventoMantenimientoActividad
        {
            DescripcionActividad = "Actividad",
            Orden = 1,
            EventoMetrologico = eventoMantenimiento
        });

        context.EventosCalibracionDato.Add(calibracion);
        context.EventosVerificacionDato.Add(verificacion);
        context.EventosMantenimientoDato.Add(mantenimiento);
    }

    private static EventoMetrologico CrearEvento(
        Equipo equipo,
        ResponsableInterno responsable,
        SubtipoEvento subtipo,
        int tipoEventoId,
        bool historico,
        bool extraordinario)
    {
        return new EventoMetrologico
        {
            Equipo = equipo,
            EquipoId = 1,
            TipoEventoMetrologicoId = tipoEventoId,
            SubtipoEvento = subtipo,
            SubtipoEventoId = 1,
            ResponsableInterno = responsable,
            ResponsableInternoId = 1,
            FechaEvento = new DateTime(2026, 6, tipoEventoId),
            EstadoEquipoResultado = "Operativo",
            EsHistorico = historico,
            EsExtraordinario = extraordinario,
            Activo = true
        };
    }
}
