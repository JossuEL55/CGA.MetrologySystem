using CGA.MetrologySystem.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize]
    public class PruebasCorreoController : Controller
    {
        private readonly IEmailService _emailService;

        public PruebasCorreoController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<IActionResult> Enviar()
        {
            await _emailService.EnviarCorreoAsync(
                "jossfarfan80@gmail.com",
                "Prueba SMTP - CGA Metrology System",
                @"
                <h2>Prueba de correo exitosa</h2>
                <p>El servicio SMTP del sistema CGA Metrology System está funcionando correctamente.</p>
                <p>Este correo fue enviado desde ASP.NET Core MVC.</p>
                "
            );

            return Content("Correo de prueba enviado correctamente.");
        }
    }
}