using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.ControlMetrologico;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Services.ControlMetrologico
{
    public class ControlMetrologicoService
    {
        private readonly AppDbContext _context;

        public ControlMetrologicoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ControlMetrologicoIndexViewModel> ObtenerVistaPorEquipoAsync(ControlMetrologicoFiltroViewModel filtros)
        {
            NormalizarFiltros(filtros);

            var equipos = await ConstruirControlesEquiposAsync(filtros.HorizonteDias);

            equipos = AplicarFiltrosEquipos(equipos, filtros);

            filtros = await CargarListasFiltrosAsync(filtros);

            return new ControlMetrologicoIndexViewModel
            {
                Filtros = filtros,
                Resumen = GenerarResumenEquipos(equipos),
                Equipos = equipos
            };
        }

        public async Task<ControlMetrologicoEventosViewModel> ObtenerVistaPorEventoAsync(ControlMetrologicoFiltroViewModel filtros)
        {
            NormalizarFiltros(filtros);

            var equipos = await ConstruirControlesEquiposAsync(filtros.HorizonteDias);

            var eventos = new List<ControlEventoOperativoViewModel>();

            foreach (var equipo in equipos)
            {
                AgregarEventoOperativo(eventos, equipo, equipo.Calibracion);
                AgregarEventoOperativo(eventos, equipo, equipo.Verificacion);
                AgregarEventoOperativo(eventos, equipo, equipo.Mantenimiento);
            }

            eventos = AplicarFiltrosEventos(eventos, filtros)
                .OrderBy(e => e.FechaProxima ?? DateTime.MaxValue)
                .ThenBy(e => e.CodigoEquipo)
                .ThenBy(e => e.TipoEventoNombre)
                .ToList();

            filtros = await CargarListasFiltrosAsync(filtros);

            return new ControlMetrologicoEventosViewModel
            {
                Filtros = filtros,
                Resumen = GenerarResumenEventos(eventos),
                Eventos = eventos
            };
        }

        private async Task<List<ControlEquipoViewModel>> ConstruirControlesEquiposAsync(int horizonteDias)
        {
            var tiposEvento = await ObtenerTiposEventoControlAsync();

            var equipos = await _context.Equipos
                .AsNoTracking()
                .Include(e => e.TipoEquipo)
                .Where(e => e.Activo)
                .OrderBy(e => e.Codigo)
                .ToListAsync();

            var configuraciones = await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .Where(c => c.Activo)
                .ToListAsync();

            var eventos = await _context.EventosMetrologicos
                .AsNoTracking()
                .Where(e => e.Activo)
                .ToListAsync();

            var controles = new List<ControlEquipoViewModel>();

            foreach (var equipo in equipos)
            {
                var calibracion = EvaluarControl(
                    equipo.EquipoId,
                    tiposEvento.CalibracionId,
                    "Calibración",
                    configuraciones,
                    eventos,
                    horizonteDias);

                var verificacion = EvaluarControl(
                    equipo.EquipoId,
                    tiposEvento.VerificacionId,
                    "Verificación",
                    configuraciones,
                    eventos,
                    horizonteDias);

                var mantenimiento = EvaluarControl(
                    equipo.EquipoId,
                    tiposEvento.MantenimientoId,
                    "Mantenimiento",
                    configuraciones,
                    eventos,
                    horizonteDias);

                var estadoGlobal = ObtenerPeorEstado(
                    calibracion.Estado,
                    verificacion.Estado,
                    mantenimiento.Estado);

                controles.Add(new ControlEquipoViewModel
                {
                    EquipoId = equipo.EquipoId,
                    CodigoEquipo = equipo.Codigo,
                    NombreEquipo = equipo.Nombre,
                    TipoEquipoId = equipo.TipoEquipoId,
                    TipoEquipo = equipo.TipoEquipo?.Nombre ?? "Sin tipo",

                    EstadoGlobal = estadoGlobal,
                    EstadoGlobalTexto = ObtenerTextoEstado(estadoGlobal),
                    EstadoGlobalCssClass = ObtenerCssEstado(estadoGlobal),
                    EstadoGlobalIcono = ObtenerIconoEstado(estadoGlobal),

                    Calibracion = calibracion,
                    Verificacion = verificacion,
                    Mantenimiento = mantenimiento
                });
            }

            return controles;
        }

        private ControlEventoViewModel EvaluarControl(
            int equipoId,
            int? tipoEventoId,
            string nombreEvento,
            List<ConfiguracionControlEquipo> configuraciones,
            List<EventoMetrologico> eventos,
            int horizonteDias)
        {
            if (!tipoEventoId.HasValue)
            {
                return CrearControl(
                    0,
                    nombreEvento,
                    EstadoControlMetrologico.SinConfiguracion,
                    null,
                    null,
                    null,
                    "No se encontró el tipo de evento en el catálogo.");
            }

            var configuracion = configuraciones
                .FirstOrDefault(c =>
                    c.EquipoId == equipoId &&
                    c.TipoEventoMetrologicoId == tipoEventoId.Value);

            if (configuracion == null)
            {
                return CrearControl(
                    tipoEventoId.Value,
                    nombreEvento,
                    EstadoControlMetrologico.SinConfiguracion,
                    null,
                    null,
                    null,
                    "No existe configuración de control para este evento.");
            }

            if (!configuracion.RequiereControl)
            {
                return CrearControl(
                    tipoEventoId.Value,
                    nombreEvento,
                    EstadoControlMetrologico.NoRequiereControl,
                    null,
                    null,
                    null,
                    "Este equipo no requiere control para este evento.");
            }

            var ultimoEvento = eventos
                .Where(e =>
                    e.EquipoId == equipoId &&
                    e.TipoEventoMetrologicoId == tipoEventoId.Value)
                .OrderByDescending(e => e.FechaEvento)
                .FirstOrDefault();

            if (ultimoEvento == null)
            {
                return CrearControl(
                    tipoEventoId.Value,
                    nombreEvento,
                    EstadoControlMetrologico.SinEventos,
                    null,
                    null,
                    null,
                    "No existen eventos registrados para este control.");
            }

            if (!ultimoEvento.FechaProxima.HasValue)
            {
                return CrearControl(
                    tipoEventoId.Value,
                    nombreEvento,
                    EstadoControlMetrologico.SinEventos,
                    ultimoEvento.FechaEvento,
                    null,
                    null,
                    "El último evento no tiene fecha próxima registrada.");
            }

            var fechaProxima = ultimoEvento.FechaProxima.Value.Date;
            var diasRestantes = (fechaProxima - DateTime.Today).Days;

            if (diasRestantes < 0)
            {
                return CrearControl(
                    tipoEventoId.Value,
                    nombreEvento,
                    EstadoControlMetrologico.Vencido,
                    ultimoEvento.FechaEvento,
                    fechaProxima,
                    diasRestantes,
                    $"Venció hace {Math.Abs(diasRestantes)} día(s).");
            }

            if (diasRestantes <= horizonteDias)
            {
                return CrearControl(
                    tipoEventoId.Value,
                    nombreEvento,
                    EstadoControlMetrologico.ProximoAVencer,
                    ultimoEvento.FechaEvento,
                    fechaProxima,
                    diasRestantes,
                    $"Vence en {diasRestantes} día(s).");
            }

            return CrearControl(
                tipoEventoId.Value,
                nombreEvento,
                EstadoControlMetrologico.Vigente,
                ultimoEvento.FechaEvento,
                fechaProxima,
                diasRestantes,
                $"Vigente hasta {fechaProxima:yyyy-MM-dd}.");
        }

        private ControlEventoViewModel CrearControl(
            int tipoEventoId,
            string nombreEvento,
            EstadoControlMetrologico estado,
            DateTime? fechaUltimoEvento,
            DateTime? fechaProxima,
            int? diasRestantes,
            string mensaje)
        {
            return new ControlEventoViewModel
            {
                TipoEventoMetrologicoId = tipoEventoId,
                TipoEventoNombre = nombreEvento,
                Estado = estado,
                FechaUltimoEvento = fechaUltimoEvento,
                FechaProxima = fechaProxima,
                DiasRestantes = diasRestantes,
                Mensaje = mensaje,
                CssClass = ObtenerCssEstado(estado),
                Icono = ObtenerIconoEstado(estado)
            };
        }

        private List<ControlEquipoViewModel> AplicarFiltrosEquipos(
            List<ControlEquipoViewModel> equipos,
            ControlMetrologicoFiltroViewModel filtros)
        {
            if (!string.IsNullOrWhiteSpace(filtros.Buscar))
            {
                var buscar = filtros.Buscar.Trim().ToLower();

                equipos = equipos
                    .Where(e =>
                        e.CodigoEquipo.ToLower().Contains(buscar) ||
                        e.NombreEquipo.ToLower().Contains(buscar))
                    .ToList();
            }

            if (filtros.TipoEquipoId.HasValue)
            {
                equipos = equipos
                    .Where(e => e.TipoEquipoId == filtros.TipoEquipoId.Value)
                    .ToList();
            }

            if (filtros.TipoEventoMetrologicoId.HasValue)
            {
                equipos = equipos
                    .Where(e => ObtenerControlPorTipoEvento(e, filtros.TipoEventoMetrologicoId.Value) != null)
                    .ToList();
            }

            if (filtros.Estado.HasValue)
            {
                if (filtros.TipoEventoMetrologicoId.HasValue)
                {
                    equipos = equipos
                        .Where(e =>
                        {
                            var control = ObtenerControlPorTipoEvento(e, filtros.TipoEventoMetrologicoId.Value);
                            return control != null && control.Estado == filtros.Estado.Value;
                        })
                        .ToList();
                }
                else
                {
                    equipos = equipos
                        .Where(e => e.EstadoGlobal == filtros.Estado.Value)
                        .ToList();
                }
            }

            return equipos;
        }

        private List<ControlEventoOperativoViewModel> AplicarFiltrosEventos(
            List<ControlEventoOperativoViewModel> eventos,
            ControlMetrologicoFiltroViewModel filtros)
        {
            var query = eventos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filtros.Buscar))
            {
                var buscar = filtros.Buscar.Trim().ToLower();

                query = query.Where(e =>
                    e.CodigoEquipo.ToLower().Contains(buscar) ||
                    e.NombreEquipo.ToLower().Contains(buscar));
            }

            if (filtros.TipoEquipoId.HasValue)
            {
                query = query.Where(e => e.TipoEquipoId == filtros.TipoEquipoId.Value);
            }

            if (filtros.TipoEventoMetrologicoId.HasValue)
            {
                query = query.Where(e => e.TipoEventoMetrologicoId == filtros.TipoEventoMetrologicoId.Value);
            }

            if (filtros.Estado.HasValue)
            {
                query = query.Where(e => e.Estado == filtros.Estado.Value);
            }

            return query.ToList();
        }

        private ControlEventoViewModel? ObtenerControlPorTipoEvento(
            ControlEquipoViewModel equipo,
            int tipoEventoMetrologicoId)
        {
            if (equipo.Calibracion.TipoEventoMetrologicoId == tipoEventoMetrologicoId)
            {
                return equipo.Calibracion;
            }

            if (equipo.Verificacion.TipoEventoMetrologicoId == tipoEventoMetrologicoId)
            {
                return equipo.Verificacion;
            }

            if (equipo.Mantenimiento.TipoEventoMetrologicoId == tipoEventoMetrologicoId)
            {
                return equipo.Mantenimiento;
            }

            return null;
        }

        private void AgregarEventoOperativo(
            List<ControlEventoOperativoViewModel> eventos,
            ControlEquipoViewModel equipo,
            ControlEventoViewModel control)
        {
            eventos.Add(new ControlEventoOperativoViewModel
            {
                EquipoId = equipo.EquipoId,
                CodigoEquipo = equipo.CodigoEquipo,
                NombreEquipo = equipo.NombreEquipo,
                TipoEquipoId = equipo.TipoEquipoId,
                TipoEquipo = equipo.TipoEquipo,

                TipoEventoMetrologicoId = control.TipoEventoMetrologicoId,
                TipoEventoNombre = control.TipoEventoNombre,
                Estado = control.Estado,
                FechaUltimoEvento = control.FechaUltimoEvento,
                FechaProxima = control.FechaProxima,
                DiasRestantes = control.DiasRestantes,
                Mensaje = control.Mensaje,
                CssClass = control.CssClass,
                Icono = control.Icono
            });
        }

        private EstadoControlMetrologico ObtenerPeorEstado(params EstadoControlMetrologico[] estados)
        {
            var prioridad = new Dictionary<EstadoControlMetrologico, int>
            {
                { EstadoControlMetrologico.Vencido, 1 },
                { EstadoControlMetrologico.ProximoAVencer, 2 },
                { EstadoControlMetrologico.SinEventos, 3 },
                { EstadoControlMetrologico.SinConfiguracion, 4 },
                { EstadoControlMetrologico.Vigente, 5 },
                { EstadoControlMetrologico.NoRequiereControl, 6 }
            };

            return estados
                .OrderBy(e => prioridad[e])
                .First();
        }

        private ResumenControlMetrologicoViewModel GenerarResumenEquipos(List<ControlEquipoViewModel> equipos)
        {
            return new ResumenControlMetrologicoViewModel
            {
                TotalEquipos = equipos.Count,
                Vigentes = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.Vigente),
                ProximosAVencer = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.ProximoAVencer),
                Vencidos = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.Vencido),
                SinEventos = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.SinEventos),
                SinConfiguracion = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.SinConfiguracion),
                NoRequierenControl = equipos.Count(e => e.EstadoGlobal == EstadoControlMetrologico.NoRequiereControl)
            };
        }

        private ResumenControlMetrologicoViewModel GenerarResumenEventos(List<ControlEventoOperativoViewModel> eventos)
        {
            return new ResumenControlMetrologicoViewModel
            {
                TotalEquipos = eventos.Select(e => e.EquipoId).Distinct().Count(),
                Vigentes = eventos.Count(e => e.Estado == EstadoControlMetrologico.Vigente),
                ProximosAVencer = eventos.Count(e => e.Estado == EstadoControlMetrologico.ProximoAVencer),
                Vencidos = eventos.Count(e => e.Estado == EstadoControlMetrologico.Vencido),
                SinEventos = eventos.Count(e => e.Estado == EstadoControlMetrologico.SinEventos),
                SinConfiguracion = eventos.Count(e => e.Estado == EstadoControlMetrologico.SinConfiguracion),
                NoRequierenControl = eventos.Count(e => e.Estado == EstadoControlMetrologico.NoRequiereControl)
            };
        }

        private async Task<ControlMetrologicoFiltroViewModel> CargarListasFiltrosAsync(ControlMetrologicoFiltroViewModel filtros)
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
                    Selected = filtros.TipoEventoMetrologicoId.HasValue && t.TipoEventoMetrologicoId == filtros.TipoEventoMetrologicoId.Value
                })
                .ToListAsync();

            filtros.Estados = Enum.GetValues<EstadoControlMetrologico>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = ObtenerTextoEstado(e),
                    Selected = filtros.Estado.HasValue && filtros.Estado.Value == e
                })
                .ToList();

            filtros.Horizontes = new List<SelectListItem>
            {
                new SelectListItem { Value = "7", Text = "7 días", Selected = filtros.HorizonteDias == 7 },
                new SelectListItem { Value = "15", Text = "15 días", Selected = filtros.HorizonteDias == 15 },
                new SelectListItem { Value = "30", Text = "30 días", Selected = filtros.HorizonteDias == 30 },
                new SelectListItem { Value = "60", Text = "60 días", Selected = filtros.HorizonteDias == 60 },
                new SelectListItem { Value = "90", Text = "90 días", Selected = filtros.HorizonteDias == 90 }
            };

            return filtros;
        }

        private async Task<(int? CalibracionId, int? VerificacionId, int? MantenimientoId)> ObtenerTiposEventoControlAsync()
        {
            var tipos = await _context.TiposEventoMetrologico
                .AsNoTracking()
                .ToListAsync();

            int? calibracionId = tipos
                .FirstOrDefault(t => t.Nombre.ToLower().Contains("calibr"))
                ?.TipoEventoMetrologicoId;

            int? verificacionId = tipos
                .FirstOrDefault(t => t.Nombre.ToLower().Contains("verific"))
                ?.TipoEventoMetrologicoId;

            int? mantenimientoId = tipos
                .FirstOrDefault(t => t.Nombre.ToLower().Contains("manten"))
                ?.TipoEventoMetrologicoId;

            return (calibracionId, verificacionId, mantenimientoId);
        }

        private void NormalizarFiltros(ControlMetrologicoFiltroViewModel filtros)
        {
            if (filtros.HorizonteDias <= 0)
            {
                filtros.HorizonteDias = 30;
            }
        }

        private string ObtenerTextoEstado(EstadoControlMetrologico estado)
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

        private string ObtenerCssEstado(EstadoControlMetrologico estado)
        {
            return estado switch
            {
                EstadoControlMetrologico.Vigente => "estado-vigente",
                EstadoControlMetrologico.ProximoAVencer => "estado-proximo",
                EstadoControlMetrologico.Vencido => "estado-vencido",
                EstadoControlMetrologico.SinEventos => "estado-sin-eventos",
                EstadoControlMetrologico.SinConfiguracion => "estado-sin-configuracion",
                EstadoControlMetrologico.NoRequiereControl => "estado-no-requiere",
                _ => "estado-desconocido"
            };
        }

        private string ObtenerIconoEstado(EstadoControlMetrologico estado)
        {
            return estado switch
            {
                EstadoControlMetrologico.Vigente => "bi-check-circle-fill",
                EstadoControlMetrologico.ProximoAVencer => "bi-exclamation-triangle-fill",
                EstadoControlMetrologico.Vencido => "bi-x-circle-fill",
                EstadoControlMetrologico.SinEventos => "bi-calendar-x",
                EstadoControlMetrologico.SinConfiguracion => "bi-gear-fill",
                EstadoControlMetrologico.NoRequiereControl => "bi-dash-circle",
                _ => "bi-question-circle"
            };
        }
    }
}