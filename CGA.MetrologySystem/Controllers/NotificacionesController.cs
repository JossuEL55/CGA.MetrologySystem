using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.Notificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class NotificacionesController : Controller
    {
        private readonly AppDbContext _context;

        public NotificacionesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var fechaUltimas24Horas = DateTime.UtcNow.AddHours(-24);

            var resumen = await _context.NotificacionesEnviadas
                .AsNoTracking()
                .GroupBy(n => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Exitosas = g.Count(n => n.FueExitosa),
                    Fallidas = g.Count(n => !n.FueExitosa),
                    Ultimas24Horas = g.Count(n => n.FechaEnvio >= fechaUltimas24Horas)
                })
                .FirstOrDefaultAsync();

            var notificaciones = await _context.NotificacionesEnviadas
                .AsNoTracking()
                .Include(n => n.Equipo)
                .OrderByDescending(n => n.FechaEnvio)
                .Take(100)
                .Select(n => new NotificacionEnviadaListadoViewModel
                {
                    NotificacionEnviadaId = n.NotificacionEnviadaId,
                    CodigoEquipo = n.Equipo.Codigo,
                    NombreEquipo = n.Equipo.Nombre,
                    TipoNotificacion = n.TipoNotificacion,
                    TipoEvento = n.TipoEvento,
                    FechaReferencia = n.FechaReferencia,
                    FechaEnvio = n.FechaEnvio,
                    Destinatarios = n.Destinatarios ?? string.Empty,
                    Mensaje = n.Mensaje ?? string.Empty,
                    FueExitosa = n.FueExitosa
                })
                .ToListAsync();

            return View(new NotificacionesIndexViewModel
            {
                TotalNotificaciones = resumen?.Total ?? 0,
                NotificacionesExitosas = resumen?.Exitosas ?? 0,
                NotificacionesFallidas = resumen?.Fallidas ?? 0,
                NotificacionesUltimas24Horas = resumen?.Ultimas24Horas ?? 0,
                Notificaciones = notificaciones
            });
        }
    }
}
