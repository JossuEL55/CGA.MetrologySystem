using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.ControlMetrologico;
using CGA.MetrologySystem.Models.MaestroEquipos;
using CGA.MetrologySystem.Services.ControlMetrologico;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Services.MaestroEquipos
{
    public class MaestroEquiposService
    {
        private readonly AppDbContext _context;
        private readonly ControlMetrologicoService _controlMetrologicoService;

        public MaestroEquiposService(
            AppDbContext context,
            ControlMetrologicoService controlMetrologicoService)
        {
            _context = context;
            _controlMetrologicoService = controlMetrologicoService;
        }

        public async Task<MaestroEquiposIndexViewModel> ObtenerIndexAsync(
            MaestroEquiposFiltroViewModel filtros)
        {
            NormalizarFiltros(filtros);

            var filtrosControl = new ControlMetrologicoFiltroViewModel
            {
                Buscar = filtros.Buscar,
                TipoEquipoId = filtros.TipoEquipoId,
                HorizonteDias = filtros.HorizonteDias
            };

            var vistaControl = await _controlMetrologicoService.ObtenerVistaPorEquipoAsync(filtrosControl);
            var vistaScore = await _controlMetrologicoService.ObtenerVistaScoreAsync(filtrosControl);
            var configuraciones = await ObtenerConfiguracionesAsync();

            var equipos = vistaControl.Equipos
                .Select(e => MapearEquipo(e, vistaScore.Items, configuraciones))
                .ToList();

            equipos = AplicarFiltrosMaestro(equipos, filtros);

            filtros = await CargarListasFiltrosAsync(filtros);

            return new MaestroEquiposIndexViewModel
            {
                Filtros = filtros,
                Equipos = equipos,
                TotalEquipos = equipos.Count,
                TotalConConfiguracionIncompleta = equipos.Count(e => e.TieneConfiguracionIncompleta),
                TotalVencidos = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.Vencido),
                TotalProximosAVencer = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.ProximoAVencer)
            };
        }

        private static MaestroEquipoItemViewModel MapearEquipo(
            ControlEquipoViewModel equipo,
            List<ScoreMetrologicoItemViewModel> scoreItems,
            Dictionary<(int EquipoId, int TipoEventoMetrologicoId), int> configuraciones)
        {
            var controles = new List<MaestroEquipoControlItemViewModel>
            {
                MapearControl(equipo.EquipoId, equipo.Calibracion, configuraciones),
                MapearControl(equipo.EquipoId, equipo.Verificacion, configuraciones),
                MapearControl(equipo.EquipoId, equipo.Mantenimiento, configuraciones)
            };

            var scoresEquipo = scoreItems
                .Where(s => s.EquipoId == equipo.EquipoId)
                .OrderByDescending(s => s.ScoreMetrologico)
                .ToList();

            var scoreMaximo = scoresEquipo.FirstOrDefault();

            return new MaestroEquipoItemViewModel
            {
                EquipoId = equipo.EquipoId,
                CodigoEquipo = equipo.CodigoEquipo,
                NombreEquipo = equipo.NombreEquipo,
                TipoEquipoId = equipo.TipoEquipoId,
                TipoEquipo = equipo.TipoEquipo,
                EstadoGlobal = equipo.EstadoGlobal,
                EstadoGlobalTexto = equipo.EstadoGlobalTexto,
                CssEstadoGlobal = equipo.EstadoGlobalCssClass,
                IconoEstadoGlobal = equipo.EstadoGlobalIcono,
                ScoreMaximo = scoreMaximo?.ScoreMetrologico,
                PrioridadMaxima = scoreMaximo?.NivelPrioridad,
                CssPrioridadMaxima = scoreMaximo?.CssPrioridad,
                TieneConfiguracionIncompleta = controles.Any(c => c.RequiereConfiguracion),
                Controles = controles
            };
        }

        private static MaestroEquipoControlItemViewModel MapearControl(
            int equipoId,
            ControlEventoViewModel control,
            Dictionary<(int EquipoId, int TipoEventoMetrologicoId), int> configuraciones)
        {
            configuraciones.TryGetValue(
                (equipoId, control.TipoEventoMetrologicoId),
                out var configuracionId);

            return new MaestroEquipoControlItemViewModel
            {
                EquipoId = equipoId,
                TipoEventoMetrologicoId = control.TipoEventoMetrologicoId,
                ConfiguracionControlEquipoId = configuracionId == 0 ? null : configuracionId,
                TipoControl = control.TipoEventoNombre,
                Estado = control.Estado,
                EstadoTexto = ObtenerTextoEstado(control.Estado),
                CssEstado = control.CssClass,
                IconoEstado = control.Icono,
                FechaUltimoEvento = control.FechaUltimoEvento,
                FechaProxima = control.FechaProxima,
                DiasRestantes = control.DiasRestantes,
                Mensaje = control.Mensaje,
                RequiereConfiguracion = control.Estado == EstadoControlMetrologico.SinConfiguracion,
                NoRequiereControl = control.Estado == EstadoControlMetrologico.NoRequiereControl
            };
        }

        private static List<MaestroEquipoItemViewModel> AplicarFiltrosMaestro(
            List<MaestroEquipoItemViewModel> equipos,
            MaestroEquiposFiltroViewModel filtros)
        {
            var query = equipos.AsEnumerable();

            if (filtros.EstadoGlobal.HasValue)
            {
                query = query.Where(e => e.EstadoGlobal == filtros.EstadoGlobal.Value);
            }

            if (filtros.SoloConfiguracionIncompleta)
            {
                query = query.Where(e => e.TieneConfiguracionIncompleta);
            }

            return query
                .OrderByDescending(e => e.TieneConfiguracionIncompleta)
                .ThenBy(e => ObtenerPrioridadEstado(e.EstadoGlobal))
                .ThenBy(e => e.CodigoEquipo)
                .ToList();
        }

        private async Task<Dictionary<(int EquipoId, int TipoEventoMetrologicoId), int>> ObtenerConfiguracionesAsync()
        {
            var configuraciones = await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .Where(c => c.Activo)
                .Select(c => new
                {
                    c.EquipoId,
                    c.TipoEventoMetrologicoId,
                    c.ConfiguracionControlEquipoId
                })
                .ToListAsync();

            return configuraciones.ToDictionary(
                c => (c.EquipoId, c.TipoEventoMetrologicoId),
                c => c.ConfiguracionControlEquipoId);
        }

        private async Task<MaestroEquiposFiltroViewModel> CargarListasFiltrosAsync(
            MaestroEquiposFiltroViewModel filtros)
        {
            filtros.TiposEquipo = await _context.TiposEquipo
                .AsNoTracking()
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.TipoEquipoId.ToString(),
                    Text = t.Nombre,
                    Selected = filtros.TipoEquipoId.HasValue && t.TipoEquipoId == filtros.TipoEquipoId.Value
                })
                .ToListAsync();

            filtros.EstadosGlobales = Enum.GetValues<EstadoControlMetrologico>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = ObtenerTextoEstado(e),
                    Selected = filtros.EstadoGlobal.HasValue && filtros.EstadoGlobal.Value == e
                })
                .ToList();

            return filtros;
        }

        private static void NormalizarFiltros(MaestroEquiposFiltroViewModel filtros)
        {
            if (filtros.HorizonteDias <= 0)
            {
                filtros.HorizonteDias = 30;
            }
        }

        private static int ObtenerPrioridadEstado(EstadoControlMetrologico estado)
        {
            return estado switch
            {
                EstadoControlMetrologico.Vencido => 1,
                EstadoControlMetrologico.ProximoAVencer => 2,
                EstadoControlMetrologico.SinEventos => 3,
                EstadoControlMetrologico.SinConfiguracion => 4,
                EstadoControlMetrologico.Vigente => 5,
                EstadoControlMetrologico.NoRequiereControl => 6,
                _ => 7
            };
        }

        private static string ObtenerTextoEstado(EstadoControlMetrologico estado)
        {
            return estado switch
            {
                EstadoControlMetrologico.Vigente => "Vigente",
                EstadoControlMetrologico.ProximoAVencer => "Próximo a vencer",
                EstadoControlMetrologico.Vencido => "Vencido",
                EstadoControlMetrologico.SinEventos => "Sin eventos",
                EstadoControlMetrologico.SinConfiguracion => "Sin configuración",
                EstadoControlMetrologico.NoRequiereControl => "No requiere control",
                _ => "Sin estado"
            };
        }
    }
}
