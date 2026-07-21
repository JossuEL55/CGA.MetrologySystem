using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.Alertas;
using CGA.MetrologySystem.Services.Alertas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.SupervisionMetrologica)]
    public class AlertasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAlertaMetrologicaService _alertaMetrologicaService;

        public AlertasController(
            AppDbContext context,
            IAlertaMetrologicaService alertaMetrologicaService)
        {
            _context = context;
            _alertaMetrologicaService = alertaMetrologicaService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var fechaUltimas24Horas = DateTime.UtcNow.AddHours(-24);

            var resumen = await _context.AlertasEnviadas
                .AsNoTracking()
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Exitosas = g.Count(a => a.FueExitosa),
                    Fallidas = g.Count(a => !a.FueExitosa),
                    Ultimas24Horas = g.Count(a => a.FechaEnvio >= fechaUltimas24Horas)
                })
                .FirstOrDefaultAsync();

            var alertas = await _context.AlertasEnviadas
                .AsNoTracking()
                .Include(a => a.Equipo)
                .OrderByDescending(a => a.FechaEnvio)
                .Take(100)
                .Select(a => new AlertaEnviadaListadoViewModel
                {
                    AlertaEnviadaId = a.AlertaEnviadaId,
                    CodigoEquipo = a.Equipo.Codigo,
                    NombreEquipo = a.Equipo.Nombre,
                    TipoEvento = a.TipoEvento,
                    TipoAlerta = a.TipoAlerta,
                    FechaReferencia = a.FechaReferencia,
                    FechaEnvio = a.FechaEnvio,
                    Destinatarios = a.Destinatarios ?? string.Empty,
                    Mensaje = a.Mensaje ?? string.Empty,
                    FueExitosa = a.FueExitosa,
                    PuedeReintentar = !a.FueExitosa
                })
                .ToListAsync();

            var model = new AlertasIndexViewModel
            {
                TotalAlertas = resumen?.Total ?? 0,
                AlertasExitosas = resumen?.Exitosas ?? 0,
                AlertasFallidas = resumen?.Fallidas ?? 0,
                AlertasUltimas24Horas = resumen?.Ultimas24Horas ?? 0,
                Alertas = alertas
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Procesar()
        {
            var resultado = await _alertaMetrologicaService.ProcesarAlertasAsync();

            TempData["SuccessMessage"] = resultado.CrearMensajeResumen();
            return RedirectToAction("Eventos", "ControlMetrologico");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reintentar(int id)
        {
            var resultado = await _alertaMetrologicaService.ReintentarAlertaFallidaAsync(id);

            if (resultado.FueExitosa)
            {
                TempData["SuccessMessage"] = resultado.Mensaje;
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
