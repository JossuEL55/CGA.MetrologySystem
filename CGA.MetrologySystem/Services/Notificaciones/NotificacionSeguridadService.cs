using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Email;

namespace CGA.MetrologySystem.Services.Notificaciones
{
    public class NotificacionSeguridadService : INotificacionSeguridadService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IDestinatariosNotificacionService _destinatariosService;
        private readonly ILogger<NotificacionSeguridadService> _logger;

        public NotificacionSeguridadService(
            AppDbContext context,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IDestinatariosNotificacionService destinatariosService,
            ILogger<NotificacionSeguridadService> logger)
        {
            _context = context;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _destinatariosService = destinatariosService;
            _logger = logger;
        }

        public Task<bool> NotificarCambioContrasenaAsync(UsuarioSistema usuario)
        {
            return NotificarAsync(
                "Cambio de contraseña",
                usuario,
                usuario,
                "El usuario cambió su propia contraseña.");
        }

        public Task<bool> NotificarRestablecimientoContrasenaAsync(
            UsuarioSistema usuarioAfectado,
            UsuarioSistema? usuarioEjecutor)
        {
            return NotificarAsync(
                "Restablecimiento de contraseña",
                usuarioAfectado,
                usuarioEjecutor,
                "Un Administrador del Sistema restableció la contraseña del usuario.");
        }

        private async Task<bool> NotificarAsync(
            string tipoEvento,
            UsuarioSistema usuarioAfectado,
            UsuarioSistema? usuarioEjecutor,
            string detalle)
        {
            var destinatarios = new List<string>();
            var fueExitosa = false;
            var mensaje = detalle;

            try
            {
                destinatarios.AddRange(
                    await _destinatariosService.ObtenerAdministradoresSistemaAsync());
                destinatarios = destinatarios
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!destinatarios.Any())
                {
                    mensaje = "No se encontraron destinatarios válidos para la notificación de seguridad.";
                }
                else
                {
                    var fecha = DateTime.UtcNow;
                    var cuerpo = ConstruirCuerpo(
                        tipoEvento,
                        usuarioAfectado,
                        usuarioEjecutor,
                        detalle,
                        fecha);

                    await _emailService.EnviarCorreoAsync(
                        destinatarios,
                        $"Seguridad: {tipoEvento.ToLowerInvariant()}",
                        cuerpo);

                    fueExitosa = true;
                    mensaje = "Notificación de seguridad enviada.";
                }
            }
            catch (Exception ex)
            {
                mensaje = LimitarMensaje($"Error al enviar la notificación de seguridad: {ex.Message}");
                _logger.LogError(
                    ex,
                    "Falló la notificación de seguridad {TipoEvento} para el usuario {UsuarioId}.",
                    tipoEvento,
                    usuarioAfectado.Id);
            }

            try
            {
                _context.NotificacionesSeguridadEnviadas.Add(
                    new NotificacionSeguridadEnviada
                    {
                        TipoEvento = tipoEvento,
                        UsuarioAfectadoId = usuarioAfectado.Id,
                        UsuarioAfectadoCorreo = usuarioAfectado.Email ?? string.Empty,
                        UsuarioEjecutorId = usuarioEjecutor?.Id,
                        UsuarioEjecutorCorreo = usuarioEjecutor?.Email,
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
                    "No se pudo registrar la notificación de seguridad {TipoEvento} para el usuario {UsuarioId}.",
                    tipoEvento,
                    usuarioAfectado.Id);
            }

            return fueExitosa;
        }

        private string ConstruirCuerpo(
            string tipoEvento,
            UsuarioSistema usuarioAfectado,
            UsuarioSistema? usuarioEjecutor,
            string detalle,
            DateTime fecha)
        {
            var ejecutor = usuarioEjecutor == null
                ? "No identificado"
                : $"{usuarioEjecutor.NombreCompleto} ({usuarioEjecutor.Email})";

            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Evento", tipoEvento),
                new EmailTemplateRow(
                    "Usuario afectado",
                    $"{usuarioAfectado.NombreCompleto} ({usuarioAfectado.Email})"),
                new EmailTemplateRow("Ejecutado por", ejecutor),
                new EmailTemplateRow("Fecha UTC", fecha.ToString("yyyy-MM-dd HH:mm:ss"))
            });

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = tipoEvento,
                Preheader = "Se registró una operación sensible sobre una cuenta del sistema.",
                Etiqueta = "Seguridad de cuentas",
                Nivel = "advertencia",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">{System.Net.WebUtility.HtmlEncode(detalle)}</p>
                    {tabla}
                    <p style=""margin:14px 0 0;"">Si esta operación no fue autorizada, revise inmediatamente la cuenta afectada.</p>"
            });
        }

        private static string LimitarMensaje(string mensaje)
        {
            return mensaje.Length <= 500 ? mensaje : mensaje[..500];
        }
    }
}
