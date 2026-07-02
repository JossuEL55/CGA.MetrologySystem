using CGA.MetrologySystem.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CGA.MetrologySystem.Infrastructure.Identity;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.SupervisionMetrologica)]
    public class PruebasCorreoController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;

        public PruebasCorreoController(
            IEmailService emailService,
            IEmailTemplateService emailTemplateService)
        {
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<IActionResult> Enviar()
        {
            var cuerpo = _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = "Prueba de correo exitosa",
                Preheader = "El servicio SMTP del sistema está funcionando correctamente.",
                Etiqueta = "Prueba SMTP",
                Nivel = "exito",
                ContenidoHtml = @"
                    <p style=""margin:0 0 14px;"">El servicio SMTP del sistema CGA Metrology System está funcionando correctamente.</p>
                    <p style=""margin:0;"">Este mensaje permite validar remitente, logo, plantilla HTML y entrega del correo.</p>"
            });

            await _emailService.EnviarCorreoAsync(
                "jossfarfan80@gmail.com",
                "Prueba SMTP - CGA Metrology System",
                cuerpo);

            return Content("Correo de prueba enviado correctamente.");
        }
    }
}
