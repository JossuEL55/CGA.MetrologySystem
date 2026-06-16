using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Models.DashboardMetrologico;
using CGA.MetrologySystem.Services.ControlMetrologico;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CGA.MetrologySystem.Services.DashboardMetrologico
{
    public class DashboardMetrologicoService
    {
        private readonly ControlMetrologicoService _controlMetrologicoService;

        public DashboardMetrologicoService(ControlMetrologicoService controlMetrologicoService)
        {
            _controlMetrologicoService = controlMetrologicoService;
        }

        public async Task<DashboardMetrologicoViewModel> ObtenerDashboardAsync(
            DashboardMetrologicoFiltroViewModel filtros)
        {
            NormalizarFiltros(filtros);

            var filtrosControl = new ControlMetrologicoFiltroViewModel
            {
                Buscar = filtros.Buscar,
                TipoEquipoId = filtros.TipoEquipoId,
                TipoEventoMetrologicoId = filtros.TipoEventoMetrologicoId,
                HorizonteDias = filtros.HorizonteDias
            };

            var vistaEquipos = await _controlMetrologicoService.ObtenerVistaPorEquipoAsync(filtrosControl);
            var vistaEventos = await _controlMetrologicoService.ObtenerVistaPorEventoAsync(filtrosControl);
            var vistaScore = await _controlMetrologicoService.ObtenerVistaScoreAsync(filtrosControl);

            filtros = MapearFiltros(vistaEquipos.Filtros, filtros);

            return new DashboardMetrologicoViewModel
            {
                Filtros = filtros,
                Resumen = ConstruirResumen(vistaEquipos, vistaEventos, vistaScore),
                DistribucionEstados = ConstruirDistribucionEstados(vistaEquipos),
                ControlesPorTipo = ConstruirControlesPorTipo(vistaEventos),
                VencimientosPorMes = ConstruirVencimientosPorMes(vistaEventos),
                TopScore = ConstruirTopScore(vistaScore),
                ControlesCriticos = ConstruirControlesCriticos(vistaScore)
            };
        }

        private static void NormalizarFiltros(DashboardMetrologicoFiltroViewModel filtros)
        {
            if (filtros.HorizonteDias <= 0)
            {
                filtros.HorizonteDias = 30;
            }
        }

        private static DashboardMetrologicoFiltroViewModel MapearFiltros(
            ControlMetrologicoFiltroViewModel filtrosControl,
            DashboardMetrologicoFiltroViewModel filtros)
        {
            filtros.TiposEquipo = filtrosControl.TiposEquipo;
            filtros.TiposEvento = filtrosControl.TiposEvento;
            filtros.Horizontes = filtrosControl.Horizontes;

            if (!filtros.Horizontes.Any())
            {
                filtros.Horizontes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "7", Text = "7 días", Selected = filtros.HorizonteDias == 7 },
                    new SelectListItem { Value = "15", Text = "15 días", Selected = filtros.HorizonteDias == 15 },
                    new SelectListItem { Value = "30", Text = "30 días", Selected = filtros.HorizonteDias == 30 },
                    new SelectListItem { Value = "60", Text = "60 días", Selected = filtros.HorizonteDias == 60 },
                    new SelectListItem { Value = "90", Text = "90 días", Selected = filtros.HorizonteDias == 90 }
                };
            }

            return filtros;
        }

        private static ResumenDashboardMetrologicoViewModel ConstruirResumen(
            ControlMetrologicoIndexViewModel vistaEquipos,
            ControlMetrologicoEventosViewModel vistaEventos,
            ControlMetrologicoScoreViewModel vistaScore)
        {
            var equiposConControlRequerido = vistaEquipos.Equipos
                .Count(e => e.EstadoGlobal != EstadoControlMetrologico.NoRequiereControl);

            var porcentajeCumplimiento = equiposConControlRequerido == 0
                ? 0
                : (double)vistaEquipos.Resumen.Vigentes / equiposConControlRequerido * 100;

            return new ResumenDashboardMetrologicoViewModel
            {
                TotalEquipos = vistaEquipos.Resumen.TotalEquipos,
                TotalControlesEvaluados = vistaEventos.Eventos.Count,
                EquiposVigentes = vistaEquipos.Resumen.Vigentes,
                EquiposProximosAVencer = vistaEquipos.Resumen.ProximosAVencer,
                EquiposVencidos = vistaEquipos.Resumen.Vencidos,
                ControlesVencidos = vistaEventos.Resumen.Vencidos,
                ControlesProximosAVencer = vistaEventos.Resumen.ProximosAVencer,
                ControlesCriticosScore = vistaScore.ResumenScore.ControlesCriticos,
                PorcentajeCumplimiento = porcentajeCumplimiento,
                PromedioScore = vistaScore.ResumenScore.PromedioScore
            };
        }

        private static List<GraficoEstadoItemViewModel> ConstruirDistribucionEstados(
            ControlMetrologicoIndexViewModel vistaEquipos)
        {
            return new List<GraficoEstadoItemViewModel>
            {
                new() { Estado = "Vigente", Cantidad = vistaEquipos.Resumen.Vigentes },
                new() { Estado = "Próximo a vencer", Cantidad = vistaEquipos.Resumen.ProximosAVencer },
                new() { Estado = "Vencido", Cantidad = vistaEquipos.Resumen.Vencidos },
                new() { Estado = "Sin eventos", Cantidad = vistaEquipos.Resumen.SinEventos },
                new() { Estado = "Sin configuración", Cantidad = vistaEquipos.Resumen.SinConfiguracion },
                new() { Estado = "No requiere control", Cantidad = vistaEquipos.Resumen.NoRequierenControl }
            };
        }

        private static List<GraficoTipoControlItemViewModel> ConstruirControlesPorTipo(
            ControlMetrologicoEventosViewModel vistaEventos)
        {
            return vistaEventos.Eventos
                .GroupBy(e => e.TipoEventoNombre)
                .OrderBy(g => g.Key)
                .Select(g => new GraficoTipoControlItemViewModel
                {
                    TipoControl = g.Key,
                    Vigentes = g.Count(e => e.Estado == EstadoControlMetrologico.Vigente),
                    ProximosAVencer = g.Count(e => e.Estado == EstadoControlMetrologico.ProximoAVencer),
                    Vencidos = g.Count(e => e.Estado == EstadoControlMetrologico.Vencido),
                    SinEventos = g.Count(e => e.Estado == EstadoControlMetrologico.SinEventos),
                    SinConfiguracion = g.Count(e => e.Estado == EstadoControlMetrologico.SinConfiguracion)
                })
                .ToList();
        }

        private static List<GraficoVencimientoMensualItemViewModel> ConstruirVencimientosPorMes(
            ControlMetrologicoEventosViewModel vistaEventos)
        {
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var meses = Enumerable.Range(0, 6)
                .Select(i => inicioMes.AddMonths(i))
                .ToList();

            var vencimientos = vistaEventos.Eventos
                .Where(e =>
                    e.FechaProxima.HasValue &&
                    e.FechaProxima.Value.Date >= inicioMes &&
                    e.FechaProxima.Value.Date < inicioMes.AddMonths(6))
                .GroupBy(e => new DateTime(e.FechaProxima!.Value.Year, e.FechaProxima.Value.Month, 1))
                .ToDictionary(g => g.Key, g => g.Count());

            return meses
                .Select(mes => new GraficoVencimientoMensualItemViewModel
                {
                    Mes = mes.ToString("yyyy-MM"),
                    Cantidad = vencimientos.TryGetValue(mes, out var cantidad) ? cantidad : 0
                })
                .ToList();
        }

        private static List<GraficoScoreItemViewModel> ConstruirTopScore(
            ControlMetrologicoScoreViewModel vistaScore)
        {
            return vistaScore.Items
                .OrderByDescending(i => i.ScoreMetrologico)
                .ThenBy(i => i.FechaProxima ?? DateTime.MaxValue)
                .Take(10)
                .Select(i => new GraficoScoreItemViewModel
                {
                    Etiqueta = $"{i.CodigoEquipo} - {i.TipoEventoNombre}",
                    Score = i.ScoreMetrologico
                })
                .ToList();
        }

        private static List<ControlCriticoDashboardItemViewModel> ConstruirControlesCriticos(
            ControlMetrologicoScoreViewModel vistaScore)
        {
            var controles = vistaScore.Items
                .Where(i => i.NivelPrioridad == "Crítico" || i.ScoreMetrologico >= 90)
                .ToList();

            if (!controles.Any())
            {
                controles = vistaScore.Items
                    .Where(i => i.NivelPrioridad == "Alto")
                    .ToList();
            }

            return controles
                .OrderByDescending(i => i.ScoreMetrologico)
                .ThenBy(i => i.FechaProxima ?? DateTime.MaxValue)
                .Select(i => new ControlCriticoDashboardItemViewModel
                {
                    EquipoId = i.EquipoId,
                    CodigoEquipo = i.CodigoEquipo,
                    NombreEquipo = i.NombreEquipo,
                    TipoEquipo = i.TipoEquipo,
                    TipoControl = i.TipoEventoNombre,
                    Estado = i.EstadoTexto,
                    FechaProxima = i.FechaProxima,
                    DiasRestantes = i.DiasRestantes,
                    ScoreMetrologico = i.ScoreMetrologico,
                    NivelPrioridad = i.NivelPrioridad,
                    CssPrioridad = i.CssPrioridad
                })
                .ToList();
        }
    }
}
