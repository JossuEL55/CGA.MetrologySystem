using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.TendenciasMetrologicas;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Services.TendenciasMetrologicas
{
    public class TendenciasMetrologicasService
    {
        private readonly AppDbContext _context;

        public TendenciasMetrologicasService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TendenciasMetrologicasIndexViewModel> ObtenerIndexAsync(
            TendenciasMetrologicasFiltroViewModel filtros)
        {
            NormalizarFiltros(filtros);

            var desviaciones = await ObtenerDesviacionesAsync(filtros);

            var equipos = desviaciones
                .GroupBy(d => new
                {
                    d.EquipoId,
                    d.CodigoEquipo,
                    d.NombreEquipo,
                    d.TipoEquipo
                })
                .Select(g => new TendenciaEquipoItemViewModel
                {
                    EquipoId = g.Key.EquipoId,
                    CodigoEquipo = g.Key.CodigoEquipo,
                    NombreEquipo = g.Key.NombreEquipo,
                    TipoEquipo = g.Key.TipoEquipo,
                    EventosAnalizados = g.Count(),
                    DesviacionPromedio = g.Average(d => d.DesviacionDias),
                    MayorDesviacion = g.Max(d => d.DesviacionDias),
                    EventosTardios = g.Count(d => d.DesviacionDias > 0),
                    EventosExtraordinarios = g.Count(d => d.EsExtraordinario)
                })
                .OrderByDescending(e => e.DesviacionPromedio)
                .ThenByDescending(e => e.MayorDesviacion)
                .ThenBy(e => e.CodigoEquipo)
                .ToList();

            filtros = await CargarListasFiltrosAsync(filtros);

            return new TendenciasMetrologicasIndexViewModel
            {
                Filtros = filtros,
                Resumen = ConstruirResumen(desviaciones),
                Equipos = equipos
            };
        }

        public async Task<TendenciasMetrologicasDetalleViewModel?> ObtenerDetalleAsync(
            int equipoId,
            TendenciasMetrologicasFiltroViewModel filtros)
        {
            NormalizarFiltros(filtros);

            var equipo = await _context.Equipos
                .AsNoTracking()
                .Include(e => e.TipoEquipo)
                .FirstOrDefaultAsync(e => e.EquipoId == equipoId);

            if (equipo == null)
            {
                return null;
            }

            filtros.Buscar = null;
            filtros.TipoEquipoId = null;

            var filtrosCalculo = new TendenciasMetrologicasFiltroViewModel
            {
                TipoEventoMetrologicoId = filtros.TipoEventoMetrologicoId,
                FechaDesde = filtros.FechaDesde,
                FechaHasta = filtros.FechaHasta
            };

            var desviaciones = await ObtenerDesviacionesAsync(filtrosCalculo, equipoId);

            filtros = await CargarListasFiltrosAsync(filtros);

            return new TendenciasMetrologicasDetalleViewModel
            {
                EquipoId = equipo.EquipoId,
                CodigoEquipo = equipo.Codigo,
                NombreEquipo = equipo.Nombre,
                TipoEquipo = equipo.TipoEquipo?.Nombre ?? "Sin tipo",
                Filtros = filtros,
                Resumen = ConstruirResumen(desviaciones),
                Desviaciones = desviaciones
                    .OrderByDescending(d => d.FechaEvento)
                    .ThenBy(d => d.TipoControl)
                    .ToList()
            };
        }

        private async Task<List<DesviacionHistoricaItemViewModel>> ObtenerDesviacionesAsync(
            TendenciasMetrologicasFiltroViewModel filtros,
            int? equipoId = null)
        {
            var query = _context.EventosMetrologicos
                .AsNoTracking()
                .Include(e => e.Equipo)
                    .ThenInclude(e => e.TipoEquipo)
                .Include(e => e.TipoEventoMetrologico)
                .Where(e => e.Activo)
                .AsQueryable();

            if (equipoId.HasValue)
            {
                query = query.Where(e => e.EquipoId == equipoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Buscar))
            {
                var buscar = filtros.Buscar.Trim().ToLower();
                query = query.Where(e =>
                    e.Equipo.Codigo.ToLower().Contains(buscar) ||
                    e.Equipo.Nombre.ToLower().Contains(buscar));
            }

            if (filtros.TipoEquipoId.HasValue)
            {
                query = query.Where(e => e.Equipo.TipoEquipoId == filtros.TipoEquipoId.Value);
            }

            if (filtros.TipoEventoMetrologicoId.HasValue)
            {
                query = query.Where(e => e.TipoEventoMetrologicoId == filtros.TipoEventoMetrologicoId.Value);
            }

            var eventos = await query
                .OrderBy(e => e.EquipoId)
                .ThenBy(e => e.TipoEventoMetrologicoId)
                .ThenBy(e => e.FechaEvento)
                .ThenBy(e => e.EventoMetrologicoId)
                .ToListAsync();

            var desviaciones = CalcularDesviaciones(eventos);

            if (filtros.FechaDesde.HasValue)
            {
                desviaciones = desviaciones
                    .Where(d => d.FechaEvento >= filtros.FechaDesde.Value.Date)
                    .ToList();
            }

            if (filtros.FechaHasta.HasValue)
            {
                desviaciones = desviaciones
                    .Where(d => d.FechaEvento <= filtros.FechaHasta.Value.Date)
                    .ToList();
            }

            return desviaciones;
        }

        private static List<DesviacionHistoricaItemViewModel> CalcularDesviaciones(
            List<EventoMetrologico> eventos)
        {
            var desviaciones = new List<DesviacionHistoricaItemViewModel>();

            foreach (var grupo in eventos.GroupBy(e => new { e.EquipoId, e.TipoEventoMetrologicoId }))
            {
                var ordenados = grupo
                    .OrderBy(e => e.FechaEvento)
                    .ThenBy(e => e.EventoMetrologicoId)
                    .ToList();

                for (var i = 1; i < ordenados.Count; i++)
                {
                    var anterior = ordenados[i - 1];
                    var actual = ordenados[i];

                    if (!anterior.FechaProxima.HasValue)
                    {
                        continue;
                    }

                    var fechaEsperada = anterior.FechaProxima.Value.Date;
                    var fechaReal = actual.FechaEvento.Date;

                    desviaciones.Add(new DesviacionHistoricaItemViewModel
                    {
                        EventoMetrologicoId = actual.EventoMetrologicoId,
                        EquipoId = actual.EquipoId,
                        CodigoEquipo = actual.Equipo.Codigo,
                        NombreEquipo = actual.Equipo.Nombre,
                        TipoEquipo = actual.Equipo.TipoEquipo?.Nombre ?? "Sin tipo",
                        TipoEventoMetrologicoId = actual.TipoEventoMetrologicoId,
                        TipoControl = actual.TipoEventoMetrologico.Nombre,
                        FechaEvento = fechaReal,
                        FechaEsperada = fechaEsperada,
                        FechaReal = fechaReal,
                        DesviacionDias = (fechaReal - fechaEsperada).Days,
                        EsExtraordinario = actual.EsExtraordinario,
                        JustificacionExtraordinario = actual.JustificacionExtraordinario
                    });
                }
            }

            return desviaciones;
        }

        private static ResumenDesviacionesViewModel ConstruirResumen(
            List<DesviacionHistoricaItemViewModel> desviaciones)
        {
            return new ResumenDesviacionesViewModel
            {
                EquiposAnalizados = desviaciones.Select(d => d.EquipoId).Distinct().Count(),
                EventosAnalizados = desviaciones.Count,
                DesviacionPromedioGlobal = desviaciones.Any()
                    ? desviaciones.Average(d => d.DesviacionDias)
                    : 0,
                EventosTardios = desviaciones.Count(d => d.DesviacionDias > 0),
                EventosAnticipados = desviaciones.Count(d => d.DesviacionDias < 0),
                EventosATiempo = desviaciones.Count(d => d.DesviacionDias == 0),
                EventosExtraordinarios = desviaciones.Count(d => d.EsExtraordinario),
                MayorDesviacion = desviaciones.Any()
                    ? desviaciones.Max(d => d.DesviacionDias)
                    : 0
            };
        }

        private async Task<TendenciasMetrologicasFiltroViewModel> CargarListasFiltrosAsync(
            TendenciasMetrologicasFiltroViewModel filtros)
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

            filtros.TiposEvento = await _context.TiposEventoMetrologico
                .AsNoTracking()
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.TipoEventoMetrologicoId.ToString(),
                    Text = t.Nombre,
                    Selected = filtros.TipoEventoMetrologicoId.HasValue &&
                        t.TipoEventoMetrologicoId == filtros.TipoEventoMetrologicoId.Value
                })
                .ToListAsync();

            return filtros;
        }

        private static void NormalizarFiltros(TendenciasMetrologicasFiltroViewModel filtros)
        {
            if (filtros.FechaDesde.HasValue && filtros.FechaHasta.HasValue &&
                filtros.FechaDesde.Value.Date > filtros.FechaHasta.Value.Date)
            {
                (filtros.FechaDesde, filtros.FechaHasta) = (filtros.FechaHasta, filtros.FechaDesde);
            }
        }
    }
}
