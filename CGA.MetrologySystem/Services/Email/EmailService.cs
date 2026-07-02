using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using CGA.MetrologySystem.Configuration;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly EmailBrandingSettings _brandingSettings;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IOptions<SmtpSettings> smtpOptions,
            IOptions<EmailBrandingSettings> brandingOptions,
            IWebHostEnvironment environment)
        {
            _smtpSettings = smtpOptions.Value;
            _brandingSettings = brandingOptions.Value;
            _environment = environment;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            await EnviarCorreoAsync(new List<string> { destinatario }, asunto, cuerpoHtml);
        }

        public async Task EnviarCorreoAsync(
            IEnumerable<string> destinatarios,
            string asunto,
            string cuerpoHtml)
        {
            var listaDestinatarios = destinatarios
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!listaDestinatarios.Any())
                return;

            using var mensaje = new MailMessage
            {
                From = new MailAddress(_smtpSettings.SenderEmail, _smtpSettings.SenderName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            foreach (var destinatario in listaDestinatarios)
            {
                mensaje.To.Add(destinatario);
            }

            AgregarLogoEmbebidoSiAplica(mensaje, cuerpoHtml);

            using var cliente = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(
                    _smtpSettings.UserName,
                    _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl
            };

            await cliente.SendMailAsync(mensaje);
        }

        private void AgregarLogoEmbebidoSiAplica(MailMessage mensaje, string cuerpoHtml)
        {
            var contentId = string.IsNullOrWhiteSpace(_brandingSettings.EmbeddedLogoContentId)
                ? "cga-logo"
                : _brandingSettings.EmbeddedLogoContentId.Trim();

            if (!cuerpoHtml.Contains($"cid:{contentId}", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                return;
            }

            var logoPath = _brandingSettings.LogoPath.TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var rutaLogo = Path.Combine(webRoot, logoPath);
            if (!File.Exists(rutaLogo))
            {
                return;
            }

            var htmlView = AlternateView.CreateAlternateViewFromString(
                cuerpoHtml,
                null,
                MediaTypeNames.Text.Html);

            var logo = new LinkedResource(rutaLogo, MediaTypeNames.Image.Png)
            {
                ContentId = contentId,
                TransferEncoding = TransferEncoding.Base64
            };

            htmlView.LinkedResources.Add(logo);
            mensaje.AlternateViews.Add(htmlView);
        }
    }
}
