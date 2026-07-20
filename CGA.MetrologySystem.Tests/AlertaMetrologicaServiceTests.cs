using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Alertas;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Notificaciones;
using Microsoft.Extensions.Options;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class AlertaMetrologicaServiceTests
{
    [Fact]
    public async Task ProcesarAlertasAsync_SinCatalogos_ReportaTodasLasReglasSinTipoEvento()
    {
        await using var context = TestDbContextFactory.Create();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(14, resultado.ReglasEvaluadas);
        Assert.Equal(14, resultado.ReglasSinTipoEvento);
        Assert.Equal(0, resultado.ControlesEvaluados);
        Assert.Empty(email.Emails);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_Preventiva30Dias_EnviaAlResponsableYRegistraExito()
    {
        await using var context = TestDbContextFactory.Create();
        SeedControl(context, DateTime.Today.AddDays(30), "responsable@example.com");
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.AlertasCandidatas);
        Assert.Equal(1, resultado.Enviadas);
        Assert.Equal("responsable@example.com", Assert.Single(email.Emails).Recipients.Single());
        Assert.Contains("vence en 30 dias", email.Emails[0].Subject);
        var registro = Assert.Single(context.AlertasEnviadas);
        Assert.True(registro.FueExitosa);
        Assert.Equal("30Dias", registro.TipoAlerta);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_PreventivaSinResponsable_UsaAdministradoresActivosYValidos()
    {
        await using var context = TestDbContextFactory.Create();
        SeedControl(context, DateTime.Today.AddDays(15), correoResponsable: null);
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();
        var usuarios = CrearAdministradores();

        var resultado = await CrearServicio(context, email, usersByRole: usuarios).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.Enviadas);
        var enviado = Assert.Single(email.Emails);
        Assert.Equal(2, enviado.Recipients.Count);
        Assert.Contains("admin@example.com", enviado.Recipients);
        Assert.Contains("metrologia@example.com", enviado.Recipients);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_Preproduccion_ReemplazaTodosLosDestinatarios()
    {
        await using var context = TestDbContextFactory.Create();
        SeedControl(context, DateTime.Today.AddDays(7), "responsable@example.com");
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(
            context,
            email,
            destinatariosSettings: new DestinatariosNotificacionesSettings
            {
                ModoPreproduccion = true,
                PermitirCorreosRegistrados = false,
                AdministradorSistema = " seguridad@example.com ",
                AdministradorMetrologico = " QA@Example.com "
            },
            usersByRole: CrearAdministradores()).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.Enviadas);
        var destinatarios = Assert.Single(email.Emails).Recipients;
        Assert.Equal(2, destinatarios.Count);
        Assert.Contains("seguridad@example.com", destinatarios);
        Assert.Contains("qa@example.com", destinatarios);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_PreventivaDuplicadaExitosa_NoReenvia()
    {
        await using var context = TestDbContextFactory.Create();
        var fecha = DateTime.Today.AddDays(4);
        SeedControl(context, fecha, "responsable@example.com");
        context.AlertasEnviadas.Add(CrearAlertaRegistrada("4Dias", fecha, DateTime.UtcNow));
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.AlertasCandidatas);
        Assert.Equal(1, resultado.OmitidasPorDuplicado);
        Assert.Empty(email.Emails);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_Vencida_EnviaAResponsableYAdministradores()
    {
        await using var context = TestDbContextFactory.Create();
        SeedControl(context, DateTime.Today.AddDays(-3), "responsable@example.com");
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(
            context,
            email,
            usersByRole: CrearAdministradores()).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.Enviadas);
        var enviado = Assert.Single(email.Emails);
        Assert.Equal(3, enviado.Recipients.Count);
        Assert.Contains("responsable@example.com", enviado.Recipients);
        Assert.Contains("Alerta critica", enviado.Subject);
        Assert.Equal("Vencido", Assert.Single(context.AlertasEnviadas).TipoAlerta);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_VencidaEnviadaHaceMenosDeSieteDias_NoReenvia()
    {
        await using var context = TestDbContextFactory.Create();
        var fecha = DateTime.Today.AddDays(-10);
        SeedControl(context, fecha, "responsable@example.com");
        context.AlertasEnviadas.Add(CrearAlertaRegistrada("Vencido", fecha, DateTime.UtcNow.AddDays(-2)));
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.OmitidasPorDuplicado);
        Assert.Empty(email.Emails);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_VencidaEnviadaHaceSieteDias_SeReenvia()
    {
        await using var context = TestDbContextFactory.Create();
        var fecha = DateTime.Today.AddDays(-10);
        SeedControl(context, fecha, "responsable@example.com");
        context.AlertasEnviadas.Add(CrearAlertaRegistrada("Vencido", fecha, DateTime.UtcNow.AddDays(-8)));
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.Enviadas);
        Assert.Single(email.Emails);
        Assert.Equal(2, context.AlertasEnviadas.Count());
    }

    [Fact]
    public async Task ProcesarAlertasAsync_SinDestinatarios_RegistraFalloControlado()
    {
        await using var context = TestDbContextFactory.Create();
        SeedControl(context, DateTime.Today.AddDays(1), correoResponsable: "correo-invalido");
        await context.SaveChangesAsync();
        var email = new RecordingEmailService();

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.SinDestinatarios);
        Assert.Empty(email.Emails);
        var registro = Assert.Single(context.AlertasEnviadas);
        Assert.False(registro.FueExitosa);
        Assert.Contains("no existen destinatarios", registro.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_ErrorDeCorreo_RegistraErrorSinPropagarExcepcion()
    {
        await using var context = TestDbContextFactory.Create();
        SeedControl(context, DateTime.Today.AddDays(1), "responsable@example.com");
        await context.SaveChangesAsync();
        var email = new RecordingEmailService
        {
            ExceptionToThrow = new InvalidOperationException("SMTP no disponible")
        };

        var resultado = await CrearServicio(context, email).ProcesarAlertasAsync();

        Assert.Equal(1, resultado.Errores);
        var registro = Assert.Single(context.AlertasEnviadas);
        Assert.False(registro.FueExitosa);
        Assert.Contains("SMTP no disponible", registro.Mensaje);
    }

    [Fact]
    public void CrearMensajeResumen_IncluyeTodosLosContadores()
    {
        var resultado = new ResultadoProcesamientoAlertas
        {
            ControlesEvaluados = 10,
            AlertasCandidatas = 5,
            Enviadas = 3,
            OmitidasPorDuplicado = 1,
            SinDestinatarios = 1,
            Errores = 2
        };

        var mensaje = resultado.CrearMensajeResumen();

        Assert.Contains("evaluadas: 10", mensaje);
        Assert.Contains("Candidatas: 5", mensaje);
        Assert.Contains("Enviadas: 3", mensaje);
        Assert.Contains("Errores: 2", mensaje);
    }

    private static AlertaMetrologicaService CrearServicio(
        AppDbContext context,
        RecordingEmailService email,
        AlertasSettings? settings = null,
        Dictionary<string, List<UsuarioSistema>>? usersByRole = null,
        DestinatariosNotificacionesSettings? destinatariosSettings = null)
    {
        var destinatariosService = new DestinatariosNotificacionService(
            new TestUserManager(usersByRole),
            Options.Create(destinatariosSettings ?? new DestinatariosNotificacionesSettings()));

        return new AlertaMetrologicaService(
            context,
            email,
            new EmailTemplateService(Options.Create(new EmailBrandingSettings())),
            destinatariosService,
            Options.Create(settings ?? new AlertasSettings()));
    }

    private static Dictionary<string, List<UsuarioSistema>> CrearAdministradores()
    {
        return new Dictionary<string, List<UsuarioSistema>>
        {
            [RolesSistema.AdministradorSistema] = new()
            {
                new UsuarioSistema { Email = " Admin@Example.com ", Activo = true },
                new UsuarioSistema { Email = "invalido", Activo = true },
                new UsuarioSistema { Email = "inactivo@example.com", Activo = false }
            },
            [RolesSistema.AdministradorMetrologico] = new()
            {
                new UsuarioSistema { Email = "metrologia@example.com", Activo = true }
            }
        };
    }

    private static void SeedControl(AppDbContext context, DateTime fechaProxima, string? correoResponsable)
    {
        var tipoEquipo = new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" };
        var responsable = new ResponsableInterno
        {
            ResponsableInternoId = 1,
            NombreCompleto = "Responsable",
            Correo = correoResponsable,
            Activo = true
        };
        var equipo = new Equipo
        {
            EquipoId = 1,
            Codigo = "EQ-ALERTA",
            Nombre = "Equipo de alerta",
            TipoEquipoId = 1,
            TipoEquipo = tipoEquipo,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            ResponsableInterno = responsable,
            Activo = true
        };
        var tipoEvento = new TipoEventoMetrologico
        {
            TipoEventoMetrologicoId = 1,
            Nombre = "Calibración"
        };

        context.TiposEquipo.Add(tipoEquipo);
        context.ResponsablesInternos.Add(responsable);
        context.Equipos.Add(equipo);
        context.TiposEventoMetrologico.Add(tipoEvento);
        context.SubtiposEvento.Add(new SubtipoEvento
        {
            SubtipoEventoId = 1,
            Nombre = "Periódico",
            Activo = true
        });
        context.ConfiguracionesControlEquipo.Add(new ConfiguracionControlEquipo
        {
            EquipoId = 1,
            Equipo = equipo,
            TipoEventoMetrologicoId = 1,
            TipoEventoMetrologico = tipoEvento,
            PeriodicidadValor = 12,
            PeriodicidadUnidad = "meses",
            RequiereControl = true,
            Activo = true
        });
        context.EventosMetrologicos.Add(new EventoMetrologico
        {
            EquipoId = 1,
            Equipo = equipo,
            TipoEventoMetrologicoId = 1,
            TipoEventoMetrologico = tipoEvento,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            ResponsableInterno = responsable,
            FechaEvento = DateTime.Today.AddMonths(-1),
            FechaProxima = fechaProxima.Date,
            Activo = true
        });
    }

    private static AlertaEnviada CrearAlertaRegistrada(
        string tipoAlerta,
        DateTime fechaReferencia,
        DateTime fechaEnvio)
    {
        return new AlertaEnviada
        {
            EquipoId = 1,
            TipoEvento = "Calibracion",
            TipoAlerta = tipoAlerta,
            FechaReferencia = DateTime.SpecifyKind(fechaReferencia.Date, DateTimeKind.Utc),
            FechaEnvio = fechaEnvio,
            Destinatarios = "responsable@example.com",
            Mensaje = "Enviada",
            FueExitosa = true
        };
    }
}
