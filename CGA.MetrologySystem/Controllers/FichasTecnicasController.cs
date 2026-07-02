using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.FichasTecnicas;
using CGA.MetrologySystem.Services.FichasTecnicas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.TodosOperativos)]
    public class FichasTecnicasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FichaTecnicaEquipoService _fichaTecnicaEquipoService;

        public FichasTecnicasController(
            AppDbContext context,
            FichaTecnicaEquipoService fichaTecnicaEquipoService)
        {
            _context = context;
            _fichaTecnicaEquipoService = fichaTecnicaEquipoService;
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
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> GenerarActualizar(int equipoId)
        {
            try
            {
                var ficha = await _fichaTecnicaEquipoService.GenerarOActualizarAsync(equipoId);

                TempData["Success"] = "La ficha técnica fue generada o actualizada correctamente.";

                return RedirectToAction(nameof(Index), new { equipoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"No se pudo generar la ficha técnica. Detalle: {ex.Message}";
                return RedirectToAction(nameof(Index), new { equipoId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerPdf(int id)
        {
            var ficha = await _context.FichasTecnicasEquipo
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FichaTecnicaEquipoId == id && f.Activa);

            if (ficha == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(ficha.GoogleDriveFileId))
            {
                TempData["Error"] = "La ficha técnica no tiene un enlace PDF disponible.";
                return RedirectToAction(nameof(Index), new { equipoId = ficha.EquipoId });
            }

            return RedirectToAction(
                "VerPdf",
                "Documentos",
                new { tipo = "ficha-tecnica", id = ficha.FichaTecnicaEquipoId });
        }

        private async Task<FichasTecnicasIndexViewModel> ConstruirIndexViewModelAsync(
            string? buscar,
            int? equipoId)
        {
            var equiposQuery = _context.Equipos
                .AsNoTracking()
                .Include(e => e.TipoEquipo)
                .Include(e => e.FichaTecnica)
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

            var fichas = await equiposQuery
                .OrderBy(e => e.Codigo)
                .Select(e => new FichaTecnicaEquipoItemViewModel
                {
                    EquipoId = e.EquipoId,
                    FichaTecnicaEquipoId = e.FichaTecnica != null
                        ? e.FichaTecnica.FichaTecnicaEquipoId
                        : null,

                    CodigoEquipo = e.Codigo,
                    NombreEquipo = e.Nombre,
                    TipoEquipo = e.TipoEquipo.Nombre,

                    TieneFichaTecnica = e.FichaTecnica != null && e.FichaTecnica.Activa,

                    NombreArchivoPdf = e.FichaTecnica != null
                        ? e.FichaTecnica.NombreArchivoPdf
                        : null,

                    RutaPdf = e.FichaTecnica != null
                        ? e.FichaTecnica.RutaPdf
                        : null,

                    FechaUltimaGeneracion = e.FichaTecnica != null
                        ? e.FichaTecnica.FechaUltimaGeneracion
                        : null,

                    CantidadEventosIncluidos = e.FichaTecnica != null
                        ? e.FichaTecnica.CantidadEventosIncluidos
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

            return new FichasTecnicasIndexViewModel
            {
                Buscar = buscar,
                EquipoId = equipoId,
                Equipos = equiposSelect,
                Fichas = fichas
            };
        }
    }
}
