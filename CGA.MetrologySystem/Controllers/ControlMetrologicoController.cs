using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Services.ControlMetrologico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize]
    public class ControlMetrologicoController : Controller
    {
        private readonly ControlMetrologicoService _controlMetrologicoService;

        public ControlMetrologicoController(ControlMetrologicoService controlMetrologicoService)
        {
            _controlMetrologicoService = controlMetrologicoService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? buscar,
            int? tipoEquipoId,
            int? tipoEventoMetrologicoId,
            EstadoControlMetrologico? estado,
            int horizonteDias = 30)
        {
            var filtros = new ControlMetrologicoFiltroViewModel
            {
                Buscar = buscar,
                TipoEquipoId = tipoEquipoId,
                TipoEventoMetrologicoId = tipoEventoMetrologicoId,
                Estado = estado,
                HorizonteDias = horizonteDias
            };

            var model = await _controlMetrologicoService.ObtenerVistaPorEquipoAsync(filtros);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Eventos(
            string? buscar,
            int? tipoEquipoId,
            int? tipoEventoMetrologicoId,
            EstadoControlMetrologico? estado,
            int horizonteDias = 30)
        {
            var filtros = new ControlMetrologicoFiltroViewModel
            {
                Buscar = buscar,
                TipoEquipoId = tipoEquipoId,
                TipoEventoMetrologicoId = tipoEventoMetrologicoId,
                Estado = estado,
                HorizonteDias = horizonteDias
            };

            var model = await _controlMetrologicoService.ObtenerVistaPorEventoAsync(filtros);

            return View(model);
        }

        [HttpGet]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult LimpiarFiltrosEventos()
        {
            return RedirectToAction(nameof(Eventos));
        }
    }
}