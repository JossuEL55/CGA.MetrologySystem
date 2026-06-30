using System.Net;
using System.Net.Mail;
using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Notificaciones
{
    public class NotificacionMetrologicaService : INotificacionMetrologicaService
    {
        private const string RolAdministrador = "Administrador";

        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<UsuarioSistema> _userManager;
        private readonly NotificacionesSettings _settings;
        private readonly ILogger<NotificacionMetrologicaService> _logger;

        public NotificacionMetrologicaService(
            AppDbContext context,
            IEmailService emailService,
            UserManager<UsuarioSistema> userManager,
            IOptions<NotificacionesSettings> settings,
            ILogger<NotificacionMetrologicaService> logger)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task NotificarEventoExtraordinarioAsync(int eventoMetrologicoId)
        {
            EventoMetrologico? evento = null;
            var destinatarios = new List<string>();

            try
            {
                evento = await _context.EventosMetrologicos
                    .AsNoTracking()
                    .Include(e => e.Equipo)
                    .Include(e => e.TipoEventoMetrologico)
                    .Include(e => e.ResponsableInterno)
                    .FirstOrDefaultAsync(e =>
                        e.EventoMetrologicoId == eventoMetrologicoId &&
                        e.Activo &&
                        e.EsExtraordinario);

                if (evento == null)
                {
                    return;
                }

                destinatarios = await ObtenerCorreosAdministradoresAsync();
                destinatarios = AplicarModoPrueba(destinatarios);

                if (!destinatarios.Any())
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        "No se encontraron destinatarios validos para la notificacion.",
                        fueExitosa: false);

                    _logger.LogWarning(
                        "No se notifico el evento extraordinario {EventoMetrologicoId} porque no existen destinatarios validos.",
                        eventoMetrologicoId);
                    return;
                }

                var tipoEvento = evento.TipoEventoMetrologico.Nombre;
                var asunto = $"Notificacion: evento extraordinario de {tipoEvento} para {evento.Equipo.Codigo}";
                var cuerpo = ConstruirCuerpoEventoExtraordinario(evento);

                await _emailService.EnviarCorreoAsync(destinatarios, asunto, cuerpo);
                await RegistrarNotificacionAsync(
                    evento,
                    destinatarios,
                    "Correo de evento extraordinario enviado.",
                    fueExitosa: true);

                _logger.LogInformation(
                    "Se notifico el evento extraordinario {EventoMetrologicoId} a {Destinatarios}.",
                    eventoMetrologicoId,
                    string.Join(", ", destinatarios));
            }
            catch (Exception ex)
            {
                if (evento != null)
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        LimitarMensaje($"Error al enviar la notificacion: {ex.Message}"),
                        fueExitosa: false);
                }

                _logger.LogError(
                    ex,
                    "Fallo la notificacion del evento extraordinario {EventoMetrologicoId}.",
                    eventoMetrologicoId);
            }
        }

        public async Task NotificarReemplazoCertificadoCalibracionAsync(
            int eventoCalibracionDatoId,
            string? nombreCertificadoAnterior,
            string? nombreCertificadoNuevo,
            string? usuarioResponsable)
        {
            EventoMetrologico? evento = null;
            var destinatarios = new List<string>();

            try
            {
                var calibracion = await _context.EventosCalibracionDato
                    .AsNoTracking()
                    .Include(c => c.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                    .Include(c => c.EventoMetrologico)
                        .ThenInclude(e => e.TipoEventoMetrologico)
                    .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == eventoCalibracionDatoId);

                if (calibracion == null)
                {
                    return;
                }

                evento = calibracion.EventoMetrologico;
                destinatarios = await ObtenerCorreosAdministradoresAsync();
                destinatarios = AplicarModoPrueba(destinatarios);

                if (!destinatarios.Any())
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        "No se encontraron destinatarios validos para el reemplazo de certificado.",
                        fueExitosa: false,
                        tipoNotificacion: "Reemplazo de certificado");

                    _logger.LogWarning(
                        "No se notifico el reemplazo de certificado de la calibracion {EventoCalibracionDatoId} porque no existen destinatarios validos.",
                        eventoCalibracionDatoId);
                    return;
                }

                var asunto = $"Notificacion: certificado de calibracion reemplazado para {evento.Equipo.Codigo}";
                var cuerpo = ConstruirCuerpoReemplazoCertificado(
                    calibracion,
                    nombreCertificadoAnterior,
                    nombreCertificadoNuevo,
                    usuarioResponsable);

                await _emailService.EnviarCorreoAsync(destinatarios, asunto, cuerpo);
                await RegistrarNotificacionAsync(
                    evento,
                    destinatarios,
                    "Correo de reemplazo de certificado enviado.",
                    fueExitosa: true,
                    tipoNotificacion: "Reemplazo de certificado");

                _logger.LogInformation(
                    "Se notifico el reemplazo de certificado de la calibracion {EventoCalibracionDatoId} a {Destinatarios}.",
                    eventoCalibracionDatoId,
                    string.Join(", ", destinatarios));
            }
            catch (Exception ex)
            {
                if (evento != null)
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        LimitarMensaje($"Error al enviar la notificacion: {ex.Message}"),
                        fueExitosa: false,
                        tipoNotificacion: "Reemplazo de certificado");
                }

                _logger.LogError(
                    ex,
                    "Fallo la notificacion del reemplazo de certificado de la calibracion {EventoCalibracionDatoId}.",
                    eventoCalibracionDatoId);
            }
        }

        public async Task NotificarEdicionCriticaVerificacionAsync(
            int eventoVerificacionDatoId,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            if (!cambiosCriticos.Any())
            {
                return;
            }

            EventoMetrologico? evento = null;
            var destinatarios = new List<string>();

            try
            {
                var verificacion = await _context.EventosVerificacionDato
                    .AsNoTracking()
                    .Include(v => v.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                    .Include(v => v.EventoMetrologico)
                        .ThenInclude(e => e.TipoEventoMetrologico)
                    .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == eventoVerificacionDatoId);

                if (verificacion == null)
                {
                    return;
                }

                evento = verificacion.EventoMetrologico;
                destinatarios = await ObtenerCorreosAdministradoresAsync();
                destinatarios = AplicarModoPrueba(destinatarios);

                if (!destinatarios.Any())
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        "No se encontraron destinatarios validos para la edicion critica de verificacion.",
                        fueExitosa: false,
                        tipoNotificacion: "Edicion critica de verificacion");

                    _logger.LogWarning(
                        "No se notifico la edicion critica de verificacion {EventoVerificacionDatoId} porque no existen destinatarios validos.",
                        eventoVerificacionDatoId);
                    return;
                }

                var asunto = $"Notificacion: edicion critica de verificacion para {evento.Equipo.Codigo}";
                var cuerpo = ConstruirCuerpoEdicionCriticaVerificacion(
                    evento,
                    usuarioResponsable,
                    cambiosCriticos);

                await _emailService.EnviarCorreoAsync(destinatarios, asunto, cuerpo);
                await RegistrarNotificacionAsync(
                    evento,
                    destinatarios,
                    "Correo de edicion critica de verificacion enviado.",
                    fueExitosa: true,
                    tipoNotificacion: "Edicion critica de verificacion");

                _logger.LogInformation(
                    "Se notifico la edicion critica de verificacion {EventoVerificacionDatoId} a {Destinatarios}.",
                    eventoVerificacionDatoId,
                    string.Join(", ", destinatarios));
            }
            catch (Exception ex)
            {
                if (evento != null)
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        LimitarMensaje($"Error al enviar la notificacion: {ex.Message}"),
                        fueExitosa: false,
                        tipoNotificacion: "Edicion critica de verificacion");
                }

                _logger.LogError(
                    ex,
                    "Fallo la notificacion de edicion critica de verificacion {EventoVerificacionDatoId}.",
                    eventoVerificacionDatoId);
            }
        }

        public async Task NotificarEdicionCriticaMantenimientoAsync(
            int eventoMantenimientoDatoId,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            if (!cambiosCriticos.Any())
            {
                return;
            }

            EventoMetrologico? evento = null;
            var destinatarios = new List<string>();

            try
            {
                var mantenimiento = await _context.EventosMantenimientoDato
                    .AsNoTracking()
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.TipoEventoMetrologico)
                    .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == eventoMantenimientoDatoId);

                if (mantenimiento == null)
                {
                    return;
                }

                evento = mantenimiento.EventoMetrologico;
                destinatarios = await ObtenerCorreosAdministradoresAsync();
                destinatarios = AplicarModoPrueba(destinatarios);

                if (!destinatarios.Any())
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        "No se encontraron destinatarios validos para la edicion critica de mantenimiento.",
                        fueExitosa: false,
                        tipoNotificacion: "Edicion critica de mantenimiento");

                    _logger.LogWarning(
                        "No se notifico la edicion critica de mantenimiento {EventoMantenimientoDatoId} porque no existen destinatarios validos.",
                        eventoMantenimientoDatoId);
                    return;
                }

                var asunto = $"Notificacion: edicion critica de mantenimiento para {evento.Equipo.Codigo}";
                var cuerpo = ConstruirCuerpoEdicionCriticaMantenimiento(
                    evento,
                    usuarioResponsable,
                    cambiosCriticos);

                await _emailService.EnviarCorreoAsync(destinatarios, asunto, cuerpo);
                await RegistrarNotificacionAsync(
                    evento,
                    destinatarios,
                    "Correo de edicion critica de mantenimiento enviado.",
                    fueExitosa: true,
                    tipoNotificacion: "Edicion critica de mantenimiento");

                _logger.LogInformation(
                    "Se notifico la edicion critica de mantenimiento {EventoMantenimientoDatoId} a {Destinatarios}.",
                    eventoMantenimientoDatoId,
                    string.Join(", ", destinatarios));
            }
            catch (Exception ex)
            {
                if (evento != null)
                {
                    await RegistrarNotificacionAsync(
                        evento,
                        destinatarios,
                        LimitarMensaje($"Error al enviar la notificacion: {ex.Message}"),
                        fueExitosa: false,
                        tipoNotificacion: "Edicion critica de mantenimiento");
                }

                _logger.LogError(
                    ex,
                    "Fallo la notificacion de edicion critica de mantenimiento {EventoMantenimientoDatoId}.",
                    eventoMantenimientoDatoId);
            }
        }

        private async Task RegistrarNotificacionAsync(
            EventoMetrologico evento,
            List<string> destinatarios,
            string mensaje,
            bool fueExitosa,
            string tipoNotificacion = "Evento extraordinario")
        {
            try
            {
                _context.NotificacionesEnviadas.Add(new NotificacionEnviada
                {
                    EquipoId = evento.EquipoId,
                    EventoMetrologicoId = evento.EventoMetrologicoId,
                    TipoNotificacion = tipoNotificacion,
                    TipoEvento = evento.TipoEventoMetrologico.Nombre,
                    FechaReferencia = DateTime.SpecifyKind(evento.FechaEvento.Date, DateTimeKind.Utc),
                    FechaEnvio = DateTime.UtcNow,
                    Destinatarios = string.Join(", ", destinatarios),
                    Mensaje = LimitarMensaje(mensaje),
                    FueExitosa = fueExitosa
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No se pudo registrar la notificacion del evento extraordinario {EventoMetrologicoId}.",
                    evento.EventoMetrologicoId);
            }
        }

        private async Task<List<string>> ObtenerCorreosAdministradoresAsync()
        {
            var administradores = await _userManager.GetUsersInRoleAsync(RolAdministrador);

            return administradores
                .Where(u => u.Activo)
                .Select(u => u.Email)
                .Where(EsCorreoValido)
                .Select(c => c!.Trim().ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<string> AplicarModoPrueba(List<string> destinatariosOriginales)
        {
            if (!_settings.ModoPrueba || !EsCorreoValido(_settings.DestinatarioPrueba))
            {
                return destinatariosOriginales;
            }

            return new List<string>
            {
                _settings.DestinatarioPrueba.Trim().ToLower()
            };
        }

        private static string ConstruirCuerpoEventoExtraordinario(
            Domain.Entities.EventoMetrologico evento)
        {
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var tipoEvento = WebUtility.HtmlEncode(evento.TipoEventoMetrologico.Nombre);
            var responsable = WebUtility.HtmlEncode(
                evento.ResponsableInterno?.NombreCompleto ?? "Sin responsable interno");
            var justificacion = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(evento.JustificacionExtraordinario)
                    ? "No se registro justificacion."
                    : evento.JustificacionExtraordinario);

            return $@"
                <h2>Evento metrologico extraordinario</h2>
                <p>Se registro un evento extraordinario que requiere seguimiento administrativo.</p>
                <table cellpadding=""6"" cellspacing=""0"" border=""1"">
                    <tr><td><strong>Equipo</strong></td><td>{codigoEquipo} - {nombreEquipo}</td></tr>
                    <tr><td><strong>Tipo de evento</strong></td><td>{tipoEvento}</td></tr>
                    <tr><td><strong>Fecha del evento</strong></td><td>{evento.FechaEvento:yyyy-MM-dd}</td></tr>
                    <tr><td><strong>Proxima fecha</strong></td><td>{FormatearFecha(evento.FechaProxima)}</td></tr>
                    <tr><td><strong>Responsable interno</strong></td><td>{responsable}</td></tr>
                    <tr><td><strong>Justificacion</strong></td><td>{justificacion}</td></tr>
                </table>
                <p>Revise la trazabilidad del evento y sus evidencias asociadas en CGA Metrology System.</p>";
        }

        private static string ConstruirCuerpoReemplazoCertificado(
            EventoCalibracionDato calibracion,
            string? nombreCertificadoAnterior,
            string? nombreCertificadoNuevo,
            string? usuarioResponsable)
        {
            var evento = calibracion.EventoMetrologico;
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var numeroCertificado = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(calibracion.NumeroCertificado)
                    ? "No definido"
                    : calibracion.NumeroCertificado);
            var archivoAnterior = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(nombreCertificadoAnterior)
                    ? "No registrado"
                    : nombreCertificadoAnterior);
            var archivoNuevo = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(nombreCertificadoNuevo)
                    ? "No registrado"
                    : nombreCertificadoNuevo);
            var usuario = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Usuario no identificado"
                    : usuarioResponsable);

            return $@"
                <h2>Cambio documental critico</h2>
                <p>Se reemplazo el certificado PDF de una calibracion registrada.</p>
                <table cellpadding=""6"" cellspacing=""0"" border=""1"">
                    <tr><td><strong>Equipo</strong></td><td>{codigoEquipo} - {nombreEquipo}</td></tr>
                    <tr><td><strong>Fecha de calibracion</strong></td><td>{FormatearFecha(calibracion.FechaCalibracion)}</td></tr>
                    <tr><td><strong>Numero de certificado</strong></td><td>{numeroCertificado}</td></tr>
                    <tr><td><strong>Archivo anterior</strong></td><td>{archivoAnterior}</td></tr>
                    <tr><td><strong>Archivo nuevo</strong></td><td>{archivoNuevo}</td></tr>
                    <tr><td><strong>Usuario</strong></td><td>{usuario}</td></tr>
                </table>
                <p>Revise el historial del evento y la evidencia documental actual en CGA Metrology System.</p>";
        }

        private static string ConstruirCuerpoEdicionCriticaVerificacion(
            EventoMetrologico evento,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var usuario = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Tecnico no identificado"
                    : usuarioResponsable);
            var cambios = string.Join(
                string.Empty,
                cambiosCriticos.Select(c => $"<li>{WebUtility.HtmlEncode(c)}</li>"));

            return $@"
                <h2>Edicion critica de verificacion</h2>
                <p>Un tecnico modifico datos sensibles de una verificacion registrada.</p>
                <table cellpadding=""6"" cellspacing=""0"" border=""1"">
                    <tr><td><strong>Equipo</strong></td><td>{codigoEquipo} - {nombreEquipo}</td></tr>
                    <tr><td><strong>Fecha del evento</strong></td><td>{evento.FechaEvento:yyyy-MM-dd}</td></tr>
                    <tr><td><strong>Proxima fecha</strong></td><td>{FormatearFecha(evento.FechaProxima)}</td></tr>
                    <tr><td><strong>Usuario</strong></td><td>{usuario}</td></tr>
                </table>
                <p><strong>Cambios criticos detectados:</strong></p>
                <ul>{cambios}</ul>
                <p>Revise la bitacora metrologica y el historial de la verificacion en CGA Metrology System.</p>";
        }

        private static string ConstruirCuerpoEdicionCriticaMantenimiento(
            EventoMetrologico evento,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var usuario = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Tecnico no identificado"
                    : usuarioResponsable);
            var cambios = string.Join(
                string.Empty,
                cambiosCriticos.Select(c => $"<li>{WebUtility.HtmlEncode(c)}</li>"));

            return $@"
                <h2>Edicion critica de mantenimiento</h2>
                <p>Un tecnico modifico datos sensibles de un mantenimiento registrado.</p>
                <table cellpadding=""6"" cellspacing=""0"" border=""1"">
                    <tr><td><strong>Equipo</strong></td><td>{codigoEquipo} - {nombreEquipo}</td></tr>
                    <tr><td><strong>Fecha del evento</strong></td><td>{evento.FechaEvento:yyyy-MM-dd}</td></tr>
                    <tr><td><strong>Proxima fecha</strong></td><td>{FormatearFecha(evento.FechaProxima)}</td></tr>
                    <tr><td><strong>Usuario</strong></td><td>{usuario}</td></tr>
                </table>
                <p><strong>Cambios criticos detectados:</strong></p>
                <ul>{cambios}</ul>
                <p>Revise la bitacora metrologica y el historial del mantenimiento en CGA Metrology System.</p>";
        }

        private static string FormatearFecha(DateTime? fecha)
        {
            return fecha.HasValue
                ? fecha.Value.ToString("yyyy-MM-dd")
                : "No definida";
        }

        private static bool EsCorreoValido(string? correo)
        {
            return !string.IsNullOrWhiteSpace(correo) &&
                MailAddress.TryCreate(correo.Trim(), out _);
        }

        private static string LimitarMensaje(string mensaje)
        {
            return mensaje.Length <= 500
                ? mensaje
                : mensaje[..500];
        }
    }
}
