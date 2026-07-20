using System.Net;
using System.Net.Mail;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Services.Notificaciones
{
    public class NotificacionMetrologicaService : INotificacionMetrologicaService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IDestinatariosNotificacionService _destinatariosService;
        private readonly ILogger<NotificacionMetrologicaService> _logger;

        public NotificacionMetrologicaService(
            AppDbContext context,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IDestinatariosNotificacionService destinatariosService,
            ILogger<NotificacionMetrologicaService> logger)
        {
            _context = context;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _destinatariosService = destinatariosService;
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

                destinatarios = await _destinatariosService.ObtenerTodosAdministradoresAsync();

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
                destinatarios = await _destinatariosService.ObtenerTodosAdministradoresAsync();

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
                destinatarios = await _destinatariosService.ObtenerTodosAdministradoresAsync();

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
                destinatarios = await _destinatariosService.ObtenerTodosAdministradoresAsync();

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

        private string ConstruirCuerpoEventoExtraordinario(
            Domain.Entities.EventoMetrologico evento)
        {
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var tipoEvento = WebUtility.HtmlEncode(evento.TipoEventoMetrologico.Nombre);
            var responsable = WebUtility.HtmlEncode(
                evento.ResponsableInterno?.NombreCompleto ?? "Sin responsable interno");
            var justificacion = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(evento.JustificacionExtraordinario)
                    ? "No se registró justificación."
                    : evento.JustificacionExtraordinario);

            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Equipo", $"{evento.Equipo.Codigo} - {evento.Equipo.Nombre}"),
                new EmailTemplateRow("Tipo de evento", evento.TipoEventoMetrologico.Nombre),
                new EmailTemplateRow("Fecha del evento", evento.FechaEvento.ToString("yyyy-MM-dd")),
                new EmailTemplateRow("Próxima fecha", FormatearFecha(evento.FechaProxima)),
                new EmailTemplateRow("Responsable interno", evento.ResponsableInterno?.NombreCompleto ?? "Sin responsable interno"),
                new EmailTemplateRow("Justificacion", string.IsNullOrWhiteSpace(evento.JustificacionExtraordinario)
                    ? "No se registró justificación."
                    : evento.JustificacionExtraordinario)
            });

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = "Evento metrológico extraordinario",
                Preheader = $"Se registró un evento extraordinario para el equipo {evento.Equipo.Codigo}.",
                Etiqueta = "Notificación metrológica",
                Nivel = "advertencia",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">Se registró un evento extraordinario que requiere seguimiento administrativo.</p>
                    {tabla}
                    <p style=""margin:0;"">Revise la trazabilidad del evento y sus evidencias asociadas en CGA Metrology System.</p>"
            });
        }

        private string ConstruirCuerpoReemplazoCertificado(
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

            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Equipo", $"{evento.Equipo.Codigo} - {evento.Equipo.Nombre}"),
                new EmailTemplateRow("Fecha de calibración", FormatearFecha(calibracion.FechaCalibracion)),
                new EmailTemplateRow("Número de certificado", string.IsNullOrWhiteSpace(calibracion.NumeroCertificado)
                    ? "No definido"
                    : calibracion.NumeroCertificado),
                new EmailTemplateRow("Archivo anterior", string.IsNullOrWhiteSpace(nombreCertificadoAnterior)
                    ? "No registrado"
                    : nombreCertificadoAnterior),
                new EmailTemplateRow("Archivo nuevo", string.IsNullOrWhiteSpace(nombreCertificadoNuevo)
                    ? "No registrado"
                    : nombreCertificadoNuevo),
                new EmailTemplateRow("Usuario", string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Usuario no identificado"
                    : usuarioResponsable)
            });

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = "Cambio documental crítico",
                Preheader = $"Se reemplazó el certificado de calibración del equipo {evento.Equipo.Codigo}.",
                Etiqueta = "Notificacion documental",
                Nivel = "critico",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">Se reemplazó el certificado PDF de una calibración registrada.</p>
                    {tabla}
                    <p style=""margin:0;"">Revise el historial del evento y la evidencia documental actual en CGA Metrology System.</p>"
            });
        }

        private string ConstruirCuerpoEdicionCriticaVerificacion(
            EventoMetrologico evento,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var usuario = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Técnico no identificado"
                    : usuarioResponsable);
            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Equipo", $"{evento.Equipo.Codigo} - {evento.Equipo.Nombre}"),
                new EmailTemplateRow("Fecha del evento", evento.FechaEvento.ToString("yyyy-MM-dd")),
                new EmailTemplateRow("Próxima fecha", FormatearFecha(evento.FechaProxima)),
                new EmailTemplateRow("Usuario", string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Técnico no identificado"
                    : usuarioResponsable)
            });
            var cambios = _emailTemplateService.ConstruirLista(cambiosCriticos);

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = "Edición crítica de verificación",
                Preheader = $"Se modificaron datos sensibles de una verificación del equipo {evento.Equipo.Codigo}.",
                Etiqueta = "Cambio crítico",
                Nivel = "critico",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">Un técnico modificó datos sensibles de una verificación registrada.</p>
                    {tabla}
                    <p style=""margin:0 0 8px;""><strong>Cambios críticos detectados:</strong></p>
                    {cambios}
                    <p style=""margin:0;"">Revise la bitácora metrológica y el historial de la verificación en CGA Metrology System.</p>"
            });
        }

        private string ConstruirCuerpoEdicionCriticaMantenimiento(
            EventoMetrologico evento,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            var codigoEquipo = WebUtility.HtmlEncode(evento.Equipo.Codigo);
            var nombreEquipo = WebUtility.HtmlEncode(evento.Equipo.Nombre);
            var usuario = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Técnico no identificado"
                    : usuarioResponsable);
            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Equipo", $"{evento.Equipo.Codigo} - {evento.Equipo.Nombre}"),
                new EmailTemplateRow("Fecha del evento", evento.FechaEvento.ToString("yyyy-MM-dd")),
                new EmailTemplateRow("Próxima fecha", FormatearFecha(evento.FechaProxima)),
                new EmailTemplateRow("Usuario", string.IsNullOrWhiteSpace(usuarioResponsable)
                    ? "Técnico no identificado"
                    : usuarioResponsable)
            });
            var cambios = _emailTemplateService.ConstruirLista(cambiosCriticos);

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = "Edición crítica de mantenimiento",
                Preheader = $"Se modificaron datos sensibles de un mantenimiento del equipo {evento.Equipo.Codigo}.",
                Etiqueta = "Cambio crítico",
                Nivel = "critico",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">Un técnico modificó datos sensibles de un mantenimiento registrado.</p>
                    {tabla}
                    <p style=""margin:0 0 8px;""><strong>Cambios críticos detectados:</strong></p>
                    {cambios}
                    <p style=""margin:0;"">Revise la bitácora metrológica y el historial del mantenimiento en CGA Metrology System.</p>"
            });
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
