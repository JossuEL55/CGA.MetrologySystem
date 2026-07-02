using CGA.MetrologySystem.Models.DashboardMetrologico;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Services.DashboardMetrologico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.SupervisionMetrologica)]
    public class DashboardMetrologicoController : Controller
    {
        private readonly DashboardMetrologicoService _dashboardMetrologicoService;

        public DashboardMetrologicoController(DashboardMetrologicoService dashboardMetrologicoService)
        {
            _dashboardMetrologicoService = dashboardMetrologicoService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? buscar,
            int? tipoEquipoId,
            int? tipoEventoMetrologicoId,
            int horizonteDias = 30)
        {
            var filtros = new DashboardMetrologicoFiltroViewModel
            {
                Buscar = buscar,
                TipoEquipoId = tipoEquipoId,
                TipoEventoMetrologicoId = tipoEventoMetrologicoId,
                HorizonteDias = horizonteDias
            };

            var model = await _dashboardMetrologicoService.ObtenerDashboardAsync(filtros);

            return View(model);
        }

        [HttpGet]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
