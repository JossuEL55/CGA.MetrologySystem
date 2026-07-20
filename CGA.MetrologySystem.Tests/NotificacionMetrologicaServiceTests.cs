using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Notificaciones;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class NotificacionMetrologicaServiceTests
{
    [Fact]
    public async Task NotificarEventoExtraordinarioAsync_EventoInexistente_NoHaceNada()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService();

        await CrearServicio(context, email).NotificarEventoExtraordinarioAsync(999);

        Assert.Empty(email.Emails);
        Assert.Empty(context.NotificacionesEnviadas);
    }

    [Fact]
    public async Task NotificarEventoExtraordinarioAsync_EventoOrdinario_NoHaceNada()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: false);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarEventoExtraordinarioAsync(1);

        Assert.Empty(email.Emails);
        Assert.Empty(context.NotificacionesEnviadas);
    }

    [Fact]
    public async Task NotificarEventoExtraordinarioAsync_Preproduccion_EnviaAAmbosAdministradores()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: true);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(
                context,
                email,
                new DestinatariosNotificacionesSettings
                {
                    ModoPreproduccion = true,
                    PermitirCorreosRegistrados = false,
                    AdministradorSistema = " seguridad@notificaciones.com ",
                    AdministradorMetrologico = " QA@Notificaciones.com "
                },
                CrearAdministradores())
            .NotificarEventoExtraordinarioAsync(1);

        var enviado = Assert.Single(email.Emails);
        Assert.Equal(2, enviado.Recipients.Count);
        Assert.Contains("seguridad@notificaciones.com", enviado.Recipients);
        Assert.Contains("qa@notificaciones.com", enviado.Recipients);
        Assert.Contains("evento extraordinario", enviado.Subject, StringComparison.OrdinalIgnoreCase);
        var registro = Assert.Single(context.NotificacionesEnviadas);
        Assert.True(registro.FueExitosa);
        Assert.Equal("Evento extraordinario", registro.TipoNotificacion);
    }

    [Fact]
    public async Task NotificarEventoExtraordinarioAsync_SinDestinatarios_RegistraFallo()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: true);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email).NotificarEventoExtraordinarioAsync(1);

        Assert.Empty(email.Emails);
        var registro = Assert.Single(context.NotificacionesEnviadas);
        Assert.False(registro.FueExitosa);
        Assert.Contains("No se encontraron destinatarios", registro.Mensaje);
    }

    [Fact]
    public async Task NotificarEventoExtraordinarioAsync_ErrorCorreo_RegistraFalloSinPropagar()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: true);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService
        {
            ExceptionToThrow = new InvalidOperationException("Proveedor SMTP caído")
        };

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarEventoExtraordinarioAsync(1);

        var registro = Assert.Single(context.NotificacionesEnviadas);
        Assert.False(registro.FueExitosa);
        Assert.Contains("SMTP caído", registro.Mensaje);
    }

    [Fact]
    public async Task NotificarReemplazoCertificadoCalibracionAsync_RegistroInexistente_NoHaceNada()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService();

        await CrearServicio(context, email).NotificarReemplazoCertificadoCalibracionAsync(
            999,
            "anterior.pdf",
            "nuevo.pdf",
            "Técnico");

        Assert.Empty(email.Emails);
        Assert.Empty(context.NotificacionesEnviadas);
    }

    [Fact]
    public async Task NotificarReemplazoCertificadoCalibracionAsync_DatosCompletos_EnviaYRegistra()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: false);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarReemplazoCertificadoCalibracionAsync(
                1,
                "certificado-anterior.pdf",
                "certificado-nuevo.pdf",
                "Técnico Uno");

        var enviado = Assert.Single(email.Emails);
        Assert.Contains("certificado de calibracion reemplazado", enviado.Subject);
        Assert.Contains("certificado-nuevo.pdf", enviado.HtmlBody);
        var registro = Assert.Single(context.NotificacionesEnviadas);
        Assert.Equal("Reemplazo de certificado", registro.TipoNotificacion);
        Assert.True(registro.FueExitosa);
    }

    [Fact]
    public async Task NotificarReemplazoCertificadoCalibracionAsync_DatosOpcionalesVacios_UsaTextosAlternativos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: false);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarReemplazoCertificadoCalibracionAsync(1, null, " ", null);

        var html = Assert.Single(email.Emails).HtmlBody;
        Assert.Contains("No registrado", html);
        Assert.Contains("Usuario no identificado", html);
    }

    [Fact]
    public async Task NotificarEdicionCriticaVerificacionAsync_SinCambios_NoConsultaNiEnvia()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: false);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarEdicionCriticaVerificacionAsync(1, "Técnico", Array.Empty<string>());

        Assert.Empty(email.Emails);
        Assert.Empty(context.NotificacionesEnviadas);
    }

    [Fact]
    public async Task NotificarEdicionCriticaVerificacionAsync_ConCambios_EnviaListaYRegistra()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: false);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarEdicionCriticaVerificacionAsync(
                1,
                "Técnico Dos",
                new[] { "Cambio de resultado", "Cambio de fecha próxima" });

        var enviado = Assert.Single(email.Emails);
        Assert.Contains("edicion critica de verificacion", enviado.Subject);
        Assert.Contains("Cambio de resultado", enviado.HtmlBody);
        var registro = Assert.Single(context.NotificacionesEnviadas);
        Assert.Equal("Edicion critica de verificacion", registro.TipoNotificacion);
    }

    [Fact]
    public async Task NotificarEdicionCriticaMantenimientoAsync_ConCambios_EnviaYRegistra()
    {
        await using var context = TestDbContextFactory.Create();
        SeedEscenario(context, extraordinario: false);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarEdicionCriticaMantenimientoAsync(
                1,
                null,
                new[] { "Actividad eliminada", "Estado modificado" });

        var enviado = Assert.Single(email.Emails);
        Assert.Contains("edicion critica de mantenimiento", enviado.Subject);
        Assert.Contains("Técnico no identificado", WebUtility.HtmlDecode(enviado.HtmlBody));
        var registro = Assert.Single(context.NotificacionesEnviadas);
        Assert.Equal("Edicion critica de mantenimiento", registro.TipoNotificacion);
        Assert.True(registro.FueExitosa);
    }

    [Fact]
    public async Task NotificarEdicionCriticaMantenimientoAsync_RegistroInexistente_NoHaceNada()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService();

        await CrearServicio(context, email, usersByRole: CrearAdministradores())
            .NotificarEdicionCriticaMantenimientoAsync(
                999,
                "Técnico",
                new[] { "Cambio" });

        Assert.Empty(email.Emails);
        Assert.Empty(context.NotificacionesEnviadas);
    }

    private static NotificacionMetrologicaService CrearServicio(
        AppDbContext context,
        RecordingEmailService email,
        DestinatariosNotificacionesSettings? settings = null,
        Dictionary<string, List<UsuarioSistema>>? usersByRole = null)
    {
        var destinatariosService = new DestinatariosNotificacionService(
            new TestUserManager(usersByRole),
            Options.Create(settings ?? new DestinatariosNotificacionesSettings()));

        return new NotificacionMetrologicaService(
            context,
            email,
            new EmailTemplateService(Options.Create(new EmailBrandingSettings())),
            destinatariosService,
            NullLogger<NotificacionMetrologicaService>.Instance);
    }

    private static Dictionary<string, List<UsuarioSistema>> CrearAdministradores()
    {
        return new Dictionary<string, List<UsuarioSistema>>
        {
            [RolesSistema.AdministradorSistema] = new()
            {
                new UsuarioSistema { Email = "admin@example.com", Activo = true },
                new UsuarioSistema { Email = "duplicado@example.com", Activo = true }
            },
            [RolesSistema.AdministradorMetrologico] = new()
            {
                new UsuarioSistema { Email = "DUPLICADO@example.com", Activo = true },
                new UsuarioSistema { Email = "incorrecto", Activo = true },
                new UsuarioSistema { Email = "inactivo@example.com", Activo = false }
            }
        };
    }

    private static void SeedEscenario(AppDbContext context, bool extraordinario)
    {
        var equipo = new Equipo
        {
            EquipoId = 1,
            Codigo = "EQ-NOTIF",
            Nombre = "Equipo notificaciones",
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        };
        var responsable = new ResponsableInterno
        {
            ResponsableInternoId = 1,
            NombreCompleto = "Responsable Interno",
            Correo = "responsable@example.com",
            Activo = true
        };
        var tipoEvento = new TipoEventoMetrologico
        {
            TipoEventoMetrologicoId = 1,
            Nombre = "Calibración"
        };
        var evento = new EventoMetrologico
        {
            EventoMetrologicoId = 1,
            EquipoId = 1,
            Equipo = equipo,
            TipoEventoMetrologicoId = 1,
            TipoEventoMetrologico = tipoEvento,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            ResponsableInterno = responsable,
            FechaEvento = new DateTime(2026, 1, 10),
            FechaProxima = new DateTime(2027, 1, 10),
            EsExtraordinario = extraordinario,
            JustificacionExtraordinario = extraordinario ? "Condición operativa" : null,
            Activo = true
        };
        var calibracion = new EventoCalibracionDato
        {
            EventoCalibracionDatoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            NumeroCertificado = "CERT-001",
            FechaCalibracion = evento.FechaEvento,
            NombreArchivoCertificado = "certificado.pdf"
        };
        var verificacion = new EventoVerificacionDato
        {
            EventoVerificacionDatoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento
        };
        var mantenimiento = new EventoMantenimientoDato
        {
            EventoMantenimientoDatoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            TipoMantenimientoId = 1,
            TipoMantenimiento = new TipoMantenimiento { TipoMantenimientoId = 1, Nombre = "Preventivo" }
        };

        evento.EventoCalibracionDato = calibracion;
        evento.EventoVerificacionDato = verificacion;
        evento.EventoMantenimientoDato = mantenimiento;

        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" });
        context.ResponsablesInternos.Add(responsable);
        context.Equipos.Add(equipo);
        context.TiposEventoMetrologico.Add(tipoEvento);
        context.SubtiposEvento.Add(new SubtipoEvento
        {
            SubtipoEventoId = 1,
            Nombre = extraordinario ? "Extraordinario" : "Periódico",
            Activo = true
        });
        context.EventosMetrologicos.Add(evento);
        context.EventosCalibracionDato.Add(calibracion);
        context.EventosVerificacionDato.Add(verificacion);
        context.EventosMantenimientoDato.Add(mantenimiento);
    }
}
