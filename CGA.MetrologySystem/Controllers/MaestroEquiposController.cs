using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Models.MaestroEquipos;
using CGA.MetrologySystem.Services.MaestroEquipos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.TodosOperativos)]
    public class MaestroEquiposController : Controller
    {
        private readonly MaestroEquiposService _maestroEquiposService;
        private readonly MaestroEquiposExcelService _maestroEquiposExcelService;

        public MaestroEquiposController(
            MaestroEquiposService maestroEquiposService,
            MaestroEquiposExcelService maestroEquiposExcelService)
        {
            _maestroEquiposService = maestroEquiposService;
            _maestroEquiposExcelService = maestroEquiposExcelService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? buscar,
            int? tipoEquipoId,
            EstadoControlMetrologico? estadoGlobal,
            bool soloConfiguracionIncompleta = false,
            int horizonteDias = 30)
        {
            var filtros = new MaestroEquiposFiltroViewModel
            {
                Buscar = buscar,
                TipoEquipoId = tipoEquipoId,
                EstadoGlobal = estadoGlobal,
                SoloConfiguracionIncompleta = soloConfiguracionIncompleta,
                HorizonteDias = horizonteDias
            };

            var model = await _maestroEquiposService.ObtenerIndexAsync(filtros);

            CompletarAcciones(model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(
            string? buscar,
            int? tipoEquipoId,
            EstadoControlMetrologico? estadoGlobal,
            bool soloConfiguracionIncompleta = false,
            int horizonteDias = 30)
        {
            var filtros = new MaestroEquiposFiltroViewModel
            {
                Buscar = buscar,
                TipoEquipoId = tipoEquipoId,
                EstadoGlobal = estadoGlobal,
                SoloConfiguracionIncompleta = soloConfiguracionIncompleta,
                HorizonteDias = horizonteDias
            };

            var usuarioExportador = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name
                : null;
            var excel = await _maestroEquiposExcelService.GenerarListadoMaestroAsync(
                filtros,
                usuarioExportador);

            return File(excel.Contenido, excel.ContentType, excel.NombreArchivo);
        }

        [HttpGet]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction(nameof(Index));
        }

        private void CompletarAcciones(MaestroEquiposIndexViewModel model)
        {
            var puedeGestionarMetrologia = PuedeGestionarMetrologia();
            var puedeRegistrarEventosOperativos = PuedeRegistrarEventosOperativos();

            foreach (var equipo in model.Equipos)
            {
                equipo.Acciones = CrearAccionesEquipo(equipo.EquipoId, puedeGestionarMetrologia);

                foreach (var control in equipo.Controles)
                {
                    if (puedeGestionarMetrologia)
                    {
                        control.UrlConfigurar = Url.Action(
                            "Create",
                            "ConfiguracionesControlEquipo");

                        if (control.ConfiguracionControlEquipoId.HasValue)
                        {
                            control.UrlEditarConfiguracion = Url.Action(
                                "Edit",
                                "ConfiguracionesControlEquipo",
                                new { id = control.ConfiguracionControlEquipoId.Value });
                        }

                    }

                    if (PuedeRegistrarEventoDesdeMaestro(control.TipoControl, puedeGestionarMetrologia, puedeRegistrarEventosOperativos))
                    {
                        control.UrlRegistrarEvento = ObtenerUrlRegistroEvento(
                            control.TipoControl,
                            control.EquipoId);
                    }
                }
            }
        }

        private List<MaestroEquipoAccionViewModel> CrearAccionesEquipo(
            int equipoId,
            bool puedeGestionarMetrologia)
        {
            var acciones = new List<MaestroEquipoAccionViewModel>();

            AgregarAccion(
                acciones,
                "Ver detalle",
                Url.Action("Details", "Equipos", new { id = equipoId }),
                "bi-eye",
                "btn btn-sm btn-outline-primary");

            if (puedeGestionarMetrologia)
            {
                AgregarAccion(
                    acciones,
                    "Editar equipo",
                    Url.Action("Edit", "Equipos", new { id = equipoId }),
                    "bi-pencil-square",
                    "btn btn-sm btn-outline-secondary");

                AgregarAccion(
                    acciones,
                    "Configurar controles",
                    Url.Action("Index", "ConfiguracionesControlEquipo"),
                    "bi-gear-fill",
                    "btn btn-sm btn-outline-dark");
            }

            AgregarAccion(
                acciones,
                "Ficha técnica",
                Url.Action("Index", "FichasTecnicas", new { equipoId }),
                "bi-file-earmark-text",
                "btn btn-sm btn-outline-info");

            AgregarAccion(
                acciones,
                "Hoja de vida",
                Url.Action("Index", "HojasVida", new { equipoId }),
                "bi-journal-text",
                "btn btn-sm btn-outline-info");

            return acciones;
        }

        private bool PuedeGestionarMetrologia()
        {
            return !User.IsInRole(RolesSistema.AdministradorSistema) &&
                   User.IsInRole(RolesSistema.AdministradorMetrologico);
        }

        private bool PuedeRegistrarEventosOperativos()
        {
            return !User.IsInRole(RolesSistema.AdministradorSistema) &&
                   (User.IsInRole(RolesSistema.Tecnico) ||
                    User.IsInRole(RolesSistema.AdministradorMetrologico));
        }

        private static bool PuedeRegistrarEventoDesdeMaestro(
            string tipoControl,
            bool puedeGestionarMetrologia,
            bool puedeRegistrarEventosOperativos)
        {
            var tipoNormalizado = tipoControl.ToLower();

            if (tipoNormalizado.Contains("calibr"))
            {
                return puedeGestionarMetrologia;
            }

            if (tipoNormalizado.Contains("verific") || tipoNormalizado.Contains("manten"))
            {
                return puedeRegistrarEventosOperativos;
            }

            return false;
        }

        private static void AgregarAccion(
            List<MaestroEquipoAccionViewModel> acciones,
            string texto,
            string? url,
            string icono,
            string cssClass)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            acciones.Add(new MaestroEquipoAccionViewModel
            {
                Texto = texto,
                Url = url,
                Icono = icono,
                CssClass = cssClass
            });
        }

        private string? ObtenerUrlRegistroEvento(string tipoControl, int equipoId)
        {
            var tipoNormalizado = tipoControl.ToLower();

            if (tipoNormalizado.Contains("calibr"))
            {
                return Url.Action("Create", "Calibraciones");
            }

            if (tipoNormalizado.Contains("verific"))
            {
                return Url.Action("Create", "Verificaciones", new { equipoId });
            }

            if (tipoNormalizado.Contains("manten"))
            {
                return Url.Action("Create", "Mantenimientos", new { equipoId });
            }

            return null;
        }
    }
}
