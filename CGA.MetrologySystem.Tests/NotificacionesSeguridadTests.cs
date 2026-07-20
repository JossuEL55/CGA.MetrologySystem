using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Notificaciones;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class NotificacionesSeguridadTests
{
    [Fact]
    public async Task Destinatarios_Preproduccion_UsaCorreosConfiguradosYNoRegistrados()
    {
        var usuarios = new Dictionary<string, List<UsuarioSistema>>
        {
            [RolesSistema.AdministradorSistema] = new()
            {
                new UsuarioSistema { Email = "registrado@example.com", Activo = true }
            }
        };
        var service = CrearDestinatarios(
            new DestinatariosNotificacionesSettings
            {
                ModoPreproduccion = true,
                PermitirCorreosRegistrados = false,
                AdministradorSistema = " Seguridad@Example.com ",
                AdministradorMetrologico = " Metrologia@Example.com "
            },
            usuarios);

        var sistema = await service.ObtenerAdministradoresSistemaAsync();
        var metrologia = await service.ObtenerAdministradoresMetrologicosAsync();

        Assert.Equal("seguridad@example.com", Assert.Single(sistema));
        Assert.Equal("metrologia@example.com", Assert.Single(metrologia));
        var todos = await service.ObtenerTodosAdministradoresAsync();
        Assert.Equal(2, todos.Count);
        Assert.False(service.PermiteCorreosRegistrados);
    }

    [Fact]
    public async Task Destinatarios_Normal_SeparaAdministradoresPorRol()
    {
        var usuarios = new Dictionary<string, List<UsuarioSistema>>
        {
            [RolesSistema.AdministradorSistema] = new()
            {
                new UsuarioSistema { Email = "sistema@example.com", Activo = true }
            },
            [RolesSistema.AdministradorMetrologico] = new()
            {
                new UsuarioSistema { Email = "metro@example.com", Activo = true },
                new UsuarioSistema { Email = "inactivo@example.com", Activo = false }
            }
        };
        var service = CrearDestinatarios(
            new DestinatariosNotificacionesSettings
            {
                ModoPreproduccion = false,
                PermitirCorreosRegistrados = true
            },
            usuarios);

        Assert.Equal(
            "sistema@example.com",
            Assert.Single(await service.ObtenerAdministradoresSistemaAsync()));
        Assert.Equal(
            "metro@example.com",
            Assert.Single(await service.ObtenerAdministradoresMetrologicosAsync()));
    }

    [Fact]
    public async Task CambioContrasena_Preproduccion_NotificaAdminSistemaYRegistraExito()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService();
        var destinatarios = CrearDestinatarios(
            new DestinatariosNotificacionesSettings
            {
                ModoPreproduccion = true,
                PermitirCorreosRegistrados = false,
                AdministradorSistema = "seguridad@example.com"
            });
        var service = CrearServicioSeguridad(context, email, destinatarios);
        var usuario = new UsuarioSistema
        {
            Id = "usuario-1",
            NombreCompleto = "Usuario Uno",
            Email = "usuario@base.local"
        };

        var resultado = await service.NotificarCambioContrasenaAsync(usuario);

        Assert.True(resultado);
        var enviado = Assert.Single(email.Emails);
        Assert.Equal("seguridad@example.com", Assert.Single(enviado.Recipients));
        Assert.DoesNotContain("usuario@base.local", enviado.Recipients);
        var registro = Assert.Single(context.NotificacionesSeguridadEnviadas);
        Assert.True(registro.FueExitosa);
        Assert.Equal("Cambio de contraseña", registro.TipoEvento);
        Assert.Equal(usuario.Id, registro.UsuarioEjecutorId);
    }

    [Fact]
    public async Task Restablecimiento_Normal_NotificaAdminSistemaYRegistraEjecutor()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService();
        var destinatarios = CrearDestinatarios(
            new DestinatariosNotificacionesSettings
            {
                PermitirCorreosRegistrados = true
            },
            new Dictionary<string, List<UsuarioSistema>>
            {
                [RolesSistema.AdministradorSistema] = new()
                {
                    new UsuarioSistema { Email = "sistema@example.com", Activo = true }
                }
            });
        var service = CrearServicioSeguridad(context, email, destinatarios);
        var afectado = new UsuarioSistema
        {
            Id = "usuario-2",
            NombreCompleto = "Usuario Dos",
            Email = "usuario@example.com"
        };
        var ejecutor = new UsuarioSistema
        {
            Id = "admin-1",
            NombreCompleto = "Administrador",
            Email = "sistema@example.com"
        };

        var resultado = await service.NotificarRestablecimientoContrasenaAsync(
            afectado,
            ejecutor);

        Assert.True(resultado);
        var enviado = Assert.Single(email.Emails);
        Assert.Equal("sistema@example.com", Assert.Single(enviado.Recipients));
        var registro = Assert.Single(context.NotificacionesSeguridadEnviadas);
        Assert.Equal(ejecutor.Id, registro.UsuarioEjecutorId);
        Assert.Equal("Restablecimiento de contraseña", registro.TipoEvento);
    }

    [Fact]
    public async Task CambioContrasena_ErrorSmtp_RegistraFalloSinPropagar()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService
        {
            ExceptionToThrow = new InvalidOperationException("SMTP fuera de servicio")
        };
        var destinatarios = CrearDestinatarios(
            new DestinatariosNotificacionesSettings
            {
                ModoPreproduccion = true,
                AdministradorSistema = "seguridad@example.com"
            });
        var service = CrearServicioSeguridad(context, email, destinatarios);

        var resultado = await service.NotificarCambioContrasenaAsync(
            new UsuarioSistema
            {
                Id = "usuario-3",
                NombreCompleto = "Usuario Tres",
                Email = "usuario3@example.com"
            });

        Assert.False(resultado);
        var registro = Assert.Single(context.NotificacionesSeguridadEnviadas);
        Assert.False(registro.FueExitosa);
        Assert.Contains("SMTP fuera de servicio", registro.Mensaje);
    }

    private static DestinatariosNotificacionService CrearDestinatarios(
        DestinatariosNotificacionesSettings settings,
        Dictionary<string, List<UsuarioSistema>>? usersByRole = null)
    {
        return new DestinatariosNotificacionService(
            new TestUserManager(usersByRole),
            Options.Create(settings));
    }

    private static NotificacionSeguridadService CrearServicioSeguridad(
        AppDbContext context,
        RecordingEmailService email,
        IDestinatariosNotificacionService destinatarios)
    {
        return new NotificacionSeguridadService(
            context,
            email,
            new EmailTemplateService(Options.Create(new EmailBrandingSettings())),
            destinatarios,
            NullLogger<NotificacionSeguridadService>.Instance);
    }
}
