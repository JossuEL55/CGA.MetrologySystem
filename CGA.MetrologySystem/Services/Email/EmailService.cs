using System.Net;
using System.Net.Mail;
using CGA.MetrologySystem.Configuration;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Email
{
    // Servicio para enviar correos electrónicos utilizando la configuración SMTP definida en SmtpSettings (Configuration).
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpOptions)
        {
            _smtpSettings = smtpOptions.Value;
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
                .Distinct()
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

            using var cliente = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(
                    _smtpSettings.UserName,
                    _smtpSettings.Password
                ),
                EnableSsl = _smtpSettings.EnableSsl
            };

            await cliente.SendMailAsync(mensaje);
        }
    }
}