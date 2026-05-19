using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.HojasVida;
using CGA.MetrologySystem.Services.HojasVida;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize]
    public class HojasVidaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly HojaVidaEquipoService _hojaVidaEquipoService;

        public HojasVidaController(
            AppDbContext context,
            HojaVidaEquipoService hojaVidaEquipoService)
        {
            _context = context;
            _hojaVidaEquipoService = hojaVidaEquipoService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? buscar, int? equipoId)
        {
            var model = await ConstruirIndexViewModelAsync(buscar, equipoId);
            return View(model);
        }

        [HttpGet]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarActualizar(int equipoId)
        {
            try
            {
                await _hojaVidaEquipoService.GenerarOActualizarAsync(equipoId);

                TempData["Success"] = "La hoja de vida fue generada o actualizada correctamente.";

                return RedirectToAction(nameof(Index), new { equipoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"No se pudo generar la hoja de vida. Detalle: {ex.Message}";
                return RedirectToAction(nameof(Index), new { equipoId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerPdf(int id)
        {
            var hojaVida = await _context.HojasVidaEquipo
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HojaVidaEquipoId == id && h.Activa);

            if (hojaVida == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(hojaVida.RutaPdf))
            {
                TempData["Error"] = "La hoja de vida no tiene un enlace PDF disponible.";
                return RedirectToAction(nameof(Index), new { equipoId = hojaVida.EquipoId });
            }

            return Redirect(hojaVida.RutaPdf);
        }

        private async Task<HojasVidaIndexViewModel> ConstruirIndexViewModelAsync(
            string? buscar,
            int? equipoId)
        {
            var equiposQuery = _context.Equipos
                .AsNoTracking()
                .Include(e => e.TipoEquipo)
                .Include(e => e.HojaVida)
                .Where(e => e.Activo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim().ToLower();

                equiposQuery = equiposQuery.Where(e =>
                    e.Codigo.ToLower().Contains(texto) ||
                    e.Nombre.ToLower().Contains(texto) ||
                    (e.Marca != null && e.Marca.ToLower().Contains(texto)) ||
                    (e.Modelo != null && e.Modelo.ToLower().Contains(texto)) ||
                    (e.Serie != null && e.Serie.ToLower().Contains(texto)));
            }

            if (equipoId.HasValue)
            {
                equiposQuery = equiposQuery.Where(e => e.EquipoId == equipoId.Value);
            }

            var hojasVida = await equiposQuery
                .OrderBy(e => e.Codigo)
                .Select(e => new HojaVidaEquipoItemViewModel
                {
                    EquipoId = e.EquipoId,

                    HojaVidaEquipoId = e.HojaVida != null
                        ? e.HojaVida.HojaVidaEquipoId
                        : null,

                    CodigoEquipo = e.Codigo,
                    NombreEquipo = e.Nombre,
                    TipoEquipo = e.TipoEquipo.Nombre,

                    TieneHojaVida = e.HojaVida != null && e.HojaVida.Activa,

                    NombreArchivoPdf = e.HojaVida != null
                        ? e.HojaVida.NombreArchivoPdf
                        : null,

                    RutaPdf = e.HojaVida != null
                        ? e.HojaVida.RutaPdf
                        : null,

                    FechaUltimaGeneracion = e.HojaVida != null
                        ? e.HojaVida.FechaUltimaGeneracion
                        : null,

                    CantidadEventosIncluidos = e.HojaVida != null
                        ? e.HojaVida.CantidadEventosIncluidos
                        : 0
                })
                .ToListAsync();

            var equiposSelect = await _context.Equipos
                .AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.Codigo)
                .Select(e => new SelectListItem
                {
                    Value = e.EquipoId.ToString(),
                    Text = e.Codigo + " - " + e.Nombre,
                    Selected = equipoId.HasValue && e.EquipoId == equipoId.Value
                })
                .ToListAsync();

            return new HojasVidaIndexViewModel
            {
                Buscar = buscar,
                EquipoId = equipoId,
                Equipos = equiposSelect,
                HojasVida = hojasVida
            };
        }
    }
}