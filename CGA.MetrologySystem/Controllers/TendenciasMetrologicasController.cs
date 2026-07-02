using CGA.MetrologySystem.Models.TendenciasMetrologicas;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Services.TendenciasMetrologicas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.SupervisionMetrologica)]
    public class TendenciasMetrologicasController : Controller
    {
        private readonly TendenciasMetrologicasService _tendenciasMetrologicasService;

        public TendenciasMetrologicasController(TendenciasMetrologicasService tendenciasMetrologicasService)
        {
            _tendenciasMetrologicasService = tendenciasMetrologicasService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? buscar,
            int? tipoEquipoId,
            int? tipoEventoMetrologicoId,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            var filtros = new TendenciasMetrologicasFiltroViewModel
            {
                Buscar = buscar,
                TipoEquipoId = tipoEquipoId,
                TipoEventoMetrologicoId = tipoEventoMetrologicoId,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            };

            var model = await _tendenciasMetrologicasService.ObtenerIndexAsync(filtros);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(
            int equipoId,
            int? tipoEventoMetrologicoId,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            var filtros = new TendenciasMetrologicasFiltroViewModel
            {
                TipoEventoMetrologicoId = tipoEventoMetrologicoId,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            };

            var model = await _tendenciasMetrologicasService.ObtenerDetalleAsync(equipoId, filtros);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult LimpiarFiltrosDetalle(int equipoId)
        {
            return RedirectToAction(nameof(Detalle), new { equipoId });
        }
    }
}
