using CGA.MetrologySystem.Controllers;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Models.MaestroEquipos;
using CGA.MetrologySystem.Services.ControlMetrologico;
using CGA.MetrologySystem.Services.MaestroEquipos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class MaestroEquiposControllerTests
{
    [Fact]
    public async Task Index_SinEquipos_RetornaVistaConModeloVacioYFiltros()
    {
        await using var context = TestDbContextFactory.Create();
        var controller = CrearController(context);

        var result = await controller.Index("buscar", 2, EstadoControlMetrologico.Vencido, true, 60);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MaestroEquiposIndexViewModel>(view.Model);
        Assert.Empty(model.Equipos);
        Assert.Equal("buscar", model.Filtros.Buscar);
        Assert.Equal(2, model.Filtros.TipoEquipoId);
        Assert.Equal(60, model.Filtros.HorizonteDias);
    }

    [Fact]
    public async Task Index_AdministradorMetrologico_CompletaAccionesYUrlsDeTodosLosControles()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEquipoConfigurado(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, RolesSistema.AdministradorMetrologico);

        var result = await controller.Index(null, null, null, false, 30);

        var model = Assert.IsType<MaestroEquiposIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        var equipo = Assert.Single(model.Equipos);
        Assert.Equal(5, equipo.Acciones.Count);
        Assert.Contains(equipo.Acciones, a => a.Texto == "Editar equipo");
        Assert.All(equipo.Controles, c => Assert.NotNull(c.UrlConfigurar));
        Assert.All(equipo.Controles, c => Assert.NotNull(c.UrlEditarConfiguracion));
        Assert.All(equipo.Controles, c => Assert.NotNull(c.UrlRegistrarEvento));
    }

    [Fact]
    public async Task Index_Tecnico_SoloPermiteRegistrarVerificacionesYMantenimientos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEquipoConfigurado(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, RolesSistema.Tecnico);

        var result = await controller.Index(null, null, null, false, 30);

        var equipo = Assert.Single(
            Assert.IsType<MaestroEquiposIndexViewModel>(Assert.IsType<ViewResult>(result).Model).Equipos);
        Assert.Equal(3, equipo.Acciones.Count);
        Assert.DoesNotContain(equipo.Acciones, a => a.Texto == "Editar equipo");
        Assert.Null(equipo.Controles.Single(c => c.TipoControl == "Calibración").UrlRegistrarEvento);
        Assert.NotNull(equipo.Controles.Single(c => c.TipoControl == "Verificación").UrlRegistrarEvento);
        Assert.NotNull(equipo.Controles.Single(c => c.TipoControl == "Mantenimiento").UrlRegistrarEvento);
        Assert.All(equipo.Controles, c => Assert.Null(c.UrlConfigurar));
    }

    [Fact]
    public async Task Index_AdministradorSistema_NoExponeAccionesDeGestionNiRegistro()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEquipoConfigurado(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, RolesSistema.AdministradorSistema);

        var result = await controller.Index(null, null, null, false, 30);

        var equipo = Assert.Single(
            Assert.IsType<MaestroEquiposIndexViewModel>(Assert.IsType<ViewResult>(result).Model).Equipos);
        Assert.Equal(3, equipo.Acciones.Count);
        Assert.All(equipo.Controles, c =>
        {
            Assert.Null(c.UrlConfigurar);
            Assert.Null(c.UrlEditarConfiguracion);
            Assert.Null(c.UrlRegistrarEvento);
        });
    }

    [Fact]
    public async Task ExportarExcel_UsuarioAutenticado_RetornaArchivoXlsx()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEquipoConfigurado(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, RolesSistema.Tecnico, "usuario.exportador");

        var result = await controller.ExportarExcel(null, null, null, false, 30);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.NotEmpty(file.FileContents);
        Assert.EndsWith(".xlsx", file.FileDownloadName);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            file.ContentType);
    }

    [Fact]
    public void LimpiarFiltros_RedirigeAIndex()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CrearController(context);

        var result = controller.LimpiarFiltros();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(MaestroEquiposController.Index), redirect.ActionName);
    }

    private static MaestroEquiposController CrearController(
        AppDbContext context,
        string? role = null,
        string? userName = null)
    {
        var service = new MaestroEquiposService(context, new ControlMetrologicoService(context));
        var controller = new MaestroEquiposController(
            service,
            new MaestroEquiposExcelService(service));
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            claims.Add(new Claim(ClaimTypes.Name, userName));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Tests"))
            }
        };
        controller.Url = new TestUrlHelper();
        return controller;
    }

    private static void SeedEquipoConfigurado(AppDbContext context)
    {
        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" });
        context.SubtiposEvento.Add(new SubtipoEvento
        {
            SubtipoEventoId = 1,
            Nombre = "Periódico",
            Activo = true
        });
        context.Equipos.Add(new Equipo
        {
            EquipoId = 1,
            Codigo = "EQ-MAESTRO",
            Nombre = "Equipo maestro",
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
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
                ConfiguracionControlEquipoId = tipoEventoId,
                EquipoId = 1,
                TipoEventoMetrologicoId = tipoEventoId,
                PeriodicidadValor = 6,
                PeriodicidadUnidad = "meses",
                RequiereControl = true,
                Activo = true
            });
        }
    }
}
