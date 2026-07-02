using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using CGA.MetrologySystem.Services.Auditoria;
using CGA.MetrologySystem.Services.Notificaciones;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.TodosOperativos)]
    public class MantenimientosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly MantenimientoPdfService _mantenimientoPdfService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IMetrologyRulesService _metrologyRulesService;
        private readonly INotificacionMetrologicaService _notificacionMetrologicaService;
        private readonly IAuditoriaMetrologicaService _auditoriaMetrologicaService;
        private readonly UserManager<Infrastructure.Identity.UsuarioSistema> _userManager;

        public MantenimientosController(
            AppDbContext context,
            MantenimientoPdfService mantenimientoPdfService,
            IGoogleDriveService googleDriveService,
            IMetrologyRulesService metrologyRulesService,
            INotificacionMetrologicaService notificacionMetrologicaService,
            IAuditoriaMetrologicaService auditoriaMetrologicaService,
            UserManager<Infrastructure.Identity.UsuarioSistema> userManager)
        {
            _context = context;
            _mantenimientoPdfService = mantenimientoPdfService;
            _googleDriveService = googleDriveService;
            _metrologyRulesService = metrologyRulesService;
            _notificacionMetrologicaService = notificacionMetrologicaService;
            _auditoriaMetrologicaService = auditoriaMetrologicaService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? buscar,
            string? estado,
            int? subtipoEventoId,
            int? tipoMantenimientoId,
            string? clasificacion,
            DateTime? desde,
            DateTime? hasta)
        {
            var query = _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(m => m.TipoMantenimiento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                query = query.Where(m =>
                    m.EventoMetrologico.Equipo.Codigo.Contains(texto) ||
                    m.EventoMetrologico.Equipo.Nombre.Contains(texto) ||
                    m.EventoMetrologico.ResponsableInterno.NombreCompleto.Contains(texto));
            }

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(m => m.EventoMetrologico.EstadoEquipoResultado == estado);

            if (subtipoEventoId.HasValue)
                query = query.Where(m => m.EventoMetrologico.SubtipoEventoId == subtipoEventoId.Value);

            if (tipoMantenimientoId.HasValue)
                query = query.Where(m => m.TipoMantenimientoId == tipoMantenimientoId.Value);

            if (desde.HasValue)
                query = query.Where(m => m.EventoMetrologico.FechaEvento >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(m => m.EventoMetrologico.FechaEvento <= hasta.Value.Date);

            query = clasificacion switch
            {
                "historicos" => query.Where(m => m.EventoMetrologico.EsHistorico),
                "extraordinarios" => query.Where(m => m.EventoMetrologico.EsExtraordinario),
                "operativos" => query.Where(m => !m.EventoMetrologico.EsHistorico),
                _ => query
            };

            var mantenimientos = await query
                .OrderByDescending(m => m.EventoMetrologico.FechaEvento)
                .ToListAsync();

            ViewBag.Buscar = buscar;
            ViewBag.Estado = estado;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.SubtiposEvento = await CrearOpcionesSubtiposAsync(subtipoEventoId);
            ViewBag.TiposMantenimiento = await CrearOpcionesTiposMantenimientoAsync(tipoMantenimientoId);
            ViewBag.Estados = CrearOpcionesEstado(estado);
            ViewBag.Clasificaciones = CrearOpcionesClasificacion(clasificacion);
            return View(mantenimientos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento.OrderBy(a => a.Orden))
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            mantenimiento.EventoMetrologico.Evidencias =
                mantenimiento.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return View(mantenimiento);
        }

        public async Task<IActionResult> ExportarPdf(int id)
        {
            var mantenimiento = await CargarMantenimientoCompletoAsync(id);

            if (mantenimiento == null)
                return NotFound();

            var pdfBytes = _mantenimientoPdfService.Generar(mantenimiento);

            var codigoEquipo = mantenimiento.EventoMetrologico.Equipo.Codigo;
            var fecha = mantenimiento.EventoMetrologico.FechaEvento.ToString("yyyy-MM-dd");

            return File(
                pdfBytes,
                "application/pdf",
                $"Mantenimiento-{codigoEquipo}-{fecha}.pdf");
        }

        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Create(int? equipoId = null)
        {
            var model = new MantenimientoViewModel
            {
                FechaEvento = DateTime.Today,
                EquipoId = equipoId ?? 0
            };

            await CargarCombosAsync(model);

            if (equipoId.HasValue && equipoId.Value > 0)
            {
                await CargarActividadesDesdePlantillaAsync(model);
            }

            if (!model.Actividades.Any())
            {
                model.Actividades.Add(new MantenimientoActividadViewModel
                {
                    Orden = 1
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Create(MantenimientoViewModel model)
        {
            NormalizarActividades(model);

            var tipoEventoMantenimiento = await ObtenerTipoEventoMantenimientoAsync();

            await ValidarFormularioMantenimientoAsync(model, tipoEventoMantenimiento);

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var eventoMetrologico = new EventoMetrologico
                {
                    EquipoId = model.EquipoId,
                    TipoEventoMetrologicoId = tipoEventoMantenimiento!.TipoEventoMetrologicoId,
                    SubtipoEventoId = model.SubtipoEventoId,
                    ResponsableInternoId = model.ResponsableInternoId,
                    FechaEvento = model.FechaEvento.Date,
                    FechaProxima = model.FechaProxima,
                    EstadoEquipoResultado = model.EstadoEquipoResultado,
                    ComentariosAdicionales = model.ComentariosAdicionales,
                    EsHistorico = model.EsHistorico,
                    ObservacionCargaHistorica = model.ObservacionCargaHistorica,
                    EsExtraordinario = model.EsExtraordinario,
                    JustificacionExtraordinario = model.JustificacionExtraordinario,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true
                };

                _context.EventosMetrologicos.Add(eventoMetrologico);
                await _context.SaveChangesAsync();

                var mantenimientoDato = new EventoMantenimientoDato
                {
                    EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId,
                    TipoMantenimientoId = model.TipoMantenimientoId
                };

                _context.EventosMantenimientoDato.Add(mantenimientoDato);

                var codigoEquipo = await _context.Equipos
                    .Where(e => e.EquipoId == model.EquipoId)
                    .Select(e => e.Codigo)
                    .FirstAsync();

                await AgregarActividadesAlEventoAsync(
                    eventoMetrologico.EventoMetrologicoId,
                    codigoEquipo,
                    model.Actividades);

                await _context.SaveChangesAsync();

                var mantenimientoCompleto = await CargarMantenimientoCompletoAsync(
                    mantenimientoDato.EventoMantenimientoDatoId);

                if (mantenimientoCompleto == null)
                    throw new Exception("No se pudo recuperar el mantenimiento para generar el PDF.");

                await GenerarYSubirPdfAsync(mantenimientoCompleto);

                var evidencias = await SubirEvidenciasAsync(
                    eventoMetrologico.EventoMetrologicoId,
                    codigoEquipo,
                    model.Evidencias);

                if (evidencias.Any())
                    _context.EvidenciasEventoMetrologico.AddRange(evidencias);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (eventoMetrologico.EsHistorico)
                {
                    await RegistrarRegistroHistoricoAsync(mantenimientoCompleto);
                }

                await _notificacionMetrologicaService.NotificarEventoExtraordinarioAsync(
                    eventoMetrologico.EventoMetrologicoId);

                return RedirectToAction(nameof(Details), new { id = mantenimientoDato.EventoMantenimientoDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el mantenimiento.");
                await CargarCombosAsync(model);
                return View(model);
            }
        }

        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento.OrderBy(a => a.Orden))
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            var evento = mantenimiento.EventoMetrologico;

            if (!evento.Activo)
                return Forbid();

            if (evento.EsHistorico && !EsAdministrador())
                return Forbid();

            var model = new MantenimientoViewModel
            {
                EventoMetrologicoId = evento.EventoMetrologicoId,
                EventoMantenimientoDatoId = mantenimiento.EventoMantenimientoDatoId,
                EquipoId = evento.EquipoId,
                SubtipoEventoId = evento.SubtipoEventoId,
                TipoMantenimientoId = mantenimiento.TipoMantenimientoId,
                ResponsableInternoId = evento.ResponsableInternoId,
                FechaEvento = evento.FechaEvento,
                FechaProxima = evento.FechaProxima,
                EstadoEquipoResultado = evento.EstadoEquipoResultado,
                ComentariosAdicionales = evento.ComentariosAdicionales,
                EsHistorico = evento.EsHistorico,
                ObservacionCargaHistorica = evento.ObservacionCargaHistorica,
                EsExtraordinario = evento.EsExtraordinario,
                JustificacionExtraordinario = evento.JustificacionExtraordinario,
                Actividades = evento.ActividadesMantenimiento
                    .OrderBy(a => a.Orden)
                    .Select(a => new MantenimientoActividadViewModel
                    {
                        EventoMantenimientoActividadId = a.EventoMantenimientoActividadId,
                        DescripcionActividad = a.DescripcionActividad,
                        Observaciones = a.Observaciones,
                        EvidenciaNombreArchivo = a.EvidenciaNombreArchivo,
                        EvidenciaRutaArchivo = a.EvidenciaRutaArchivo,
                        Orden = a.Orden
                    })
                    .ToList(),
                EvidenciasExistentes = evento.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .Select(e => new EvidenciaEventoViewModel
                    {
                        EvidenciaEventoMetrologicoId = e.EvidenciaEventoMetrologicoId,
                        NombreArchivo = e.NombreArchivo,
                        ContentType = e.ContentType,
                        GoogleDriveFileId = e.GoogleDriveFileId,
                        RutaArchivo = e.RutaArchivo,
                        TipoEvidencia = e.TipoEvidencia,
                        Descripcion = e.Descripcion,
                        FechaCarga = e.FechaCarga,
                        Activo = e.Activo
                    })
                    .ToList()
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Edit(int id, MantenimientoViewModel model)
        {
            if (id != model.EventoMantenimientoDatoId)
                return NotFound();

            NormalizarActividades(model);

            var tipoEventoMantenimiento = await ObtenerTipoEventoMantenimientoAsync();

            await ValidarFormularioMantenimientoAsync(model, tipoEventoMantenimiento);

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                await CargarEvidenciasExistentesEnModeloAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var mantenimiento = await _context.EventosMantenimientoDato
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.ActividadesMantenimiento)
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.Evidencias)
                    .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

                if (mantenimiento == null)
                    return NotFound();

                var evento = mantenimiento.EventoMetrologico;

                if (!evento.Activo)
                    return Forbid();

                if (evento.EsHistorico && !EsAdministrador())
                    return Forbid();

                var eraHistorico = evento.EsHistorico;
                var cambiosCriticos = await DetectarCambiosCriticosAsync(evento, model);

                evento.EquipoId = model.EquipoId;
                evento.TipoEventoMetrologicoId = tipoEventoMantenimiento!.TipoEventoMetrologicoId;
                evento.SubtipoEventoId = model.SubtipoEventoId;
                evento.ResponsableInternoId = model.ResponsableInternoId;
                evento.FechaEvento = model.FechaEvento.Date;
                evento.FechaProxima = model.FechaProxima;
                evento.EstadoEquipoResultado = model.EstadoEquipoResultado;
                evento.ComentariosAdicionales = model.ComentariosAdicionales;
                evento.EsHistorico = model.EsHistorico;
                evento.ObservacionCargaHistorica = model.ObservacionCargaHistorica;
                evento.EsExtraordinario = model.EsExtraordinario;
                evento.JustificacionExtraordinario = model.JustificacionExtraordinario;

                mantenimiento.TipoMantenimientoId = model.TipoMantenimientoId;

                await ReemplazarActividadesAsync(
                    evento,
                    evento.Equipo.Codigo,
                    model.Actividades);

                await _context.SaveChangesAsync();

                var mantenimientoCompleto = await CargarMantenimientoCompletoAsync(
                    mantenimiento.EventoMantenimientoDatoId);

                if (mantenimientoCompleto == null)
                    throw new Exception("No se pudo recuperar el mantenimiento para regenerar el PDF.");

                if (!string.IsNullOrWhiteSpace(mantenimiento.GoogleDriveFileId))
                    await _googleDriveService.DeleteFileAsync(mantenimiento.GoogleDriveFileId);

                await GenerarYSubirPdfAsync(mantenimientoCompleto);

                var codigoEquipo = mantenimientoCompleto.EventoMetrologico.Equipo.Codigo;

                var nuevasEvidencias = await SubirEvidenciasAsync(
                    evento.EventoMetrologicoId,
                    codigoEquipo,
                    model.Evidencias);

                if (nuevasEvidencias.Any())
                    _context.EvidenciasEventoMetrologico.AddRange(nuevasEvidencias);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (cambiosCriticos.Any())
                {
                    await RegistrarEdicionCriticaAsync(
                        mantenimientoCompleto,
                        cambiosCriticos);
                }
                else if (EsAdministrador() && (eraHistorico || model.EsHistorico))
                {
                    await RegistrarCorreccionHistoricaAsync(mantenimientoCompleto);
                }

                return RedirectToAction(nameof(Details), new { id = mantenimiento.EventoMantenimientoDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el mantenimiento.");
                await CargarCombosAsync(model);
                await CargarEvidenciasExistentesEnModeloAsync(model);
                return View(model);
            }
        }

        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento.OrderBy(a => a.Orden))
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            if (!mantenimiento.EventoMetrologico.Activo)
                return Forbid();

            if (mantenimiento.EventoMetrologico.EsHistorico && !EsAdministrador())
                return Forbid();

            mantenimiento.EventoMetrologico.Evidencias =
                mantenimiento.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return View(mantenimiento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            if (!mantenimiento.EventoMetrologico.Activo)
                return Forbid();

            if (mantenimiento.EventoMetrologico.EsHistorico && !EsAdministrador())
                return Forbid();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var evento = mantenimiento.EventoMetrologico;

                if (!string.IsNullOrWhiteSpace(mantenimiento.GoogleDriveFileId))
                    await _googleDriveService.DeleteFileAsync(mantenimiento.GoogleDriveFileId);

                foreach (var evidencia in evento.Evidencias)
                {
                    if (!string.IsNullOrWhiteSpace(evidencia.GoogleDriveFileId))
                        await _googleDriveService.DeleteFileAsync(evidencia.GoogleDriveFileId);
                }

                _context.EventosMantenimientoActividad.RemoveRange(evento.ActividadesMantenimiento);
                _context.EvidenciasEventoMetrologico.RemoveRange(evento.Evidencias);
                _context.EventosMantenimientoDato.Remove(mantenimiento);
                _context.EventosMetrologicos.Remove(evento);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] = "Ocurrió un error al eliminar el mantenimiento.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task CargarCombosAsync(MantenimientoViewModel model)
        {
            model.Equipos = await _context.Equipos
                .Where(e => e.Activo)
                .OrderBy(e => e.Codigo)
                .Select(e => new SelectListItem
                {
                    Value = e.EquipoId.ToString(),
                    Text = e.Codigo + " - " + e.Nombre
                })
                .ToListAsync();

            model.SubtiposEvento = await _context.SubtiposEvento
                .Where(s => s.Activo)
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem
                {
                    Value = s.SubtipoEventoId.ToString(),
                    Text = s.Nombre
                })
                .ToListAsync();

            model.TiposMantenimiento = await _context.TiposMantenimiento
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.TipoMantenimientoId.ToString(),
                    Text = t.Nombre
                })
                .ToListAsync();

            model.ResponsablesInternos = await _context.ResponsablesInternos
                .Where(r => r.Activo)
                .OrderBy(r => r.NombreCompleto)
                .Select(r => new SelectListItem
                {
                    Value = r.ResponsableInternoId.ToString(),
                    Text = r.NombreCompleto
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> CrearOpcionesSubtiposAsync(int? subtipoEventoId)
        {
            return await _context.SubtiposEvento
                .AsNoTracking()
                .Where(s => s.Activo)
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem
                {
                    Value = s.SubtipoEventoId.ToString(),
                    Text = s.Nombre,
                    Selected = s.SubtipoEventoId == subtipoEventoId
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> CrearOpcionesTiposMantenimientoAsync(int? tipoMantenimientoId)
        {
            return await _context.TiposMantenimiento
                .AsNoTracking()
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.TipoMantenimientoId.ToString(),
                    Text = t.Nombre,
                    Selected = t.TipoMantenimientoId == tipoMantenimientoId
                })
                .ToListAsync();
        }

        private static List<SelectListItem> CrearOpcionesEstado(string? estado)
        {
            return new[] { "Operativo", "No Operativo", "Fuera de Servicio" }
                .Select(valor => new SelectListItem(valor, valor, valor == estado))
                .ToList();
        }

        private static List<SelectListItem> CrearOpcionesClasificacion(string? clasificacion)
        {
            return new List<SelectListItem>
            {
                new("Operativos", "operativos", clasificacion == "operativos"),
                new("Históricos", "historicos", clasificacion == "historicos"),
                new("Extraordinarios", "extraordinarios", clasificacion == "extraordinarios")
            };
        }

        private async Task CargarActividadesDesdePlantillaAsync(MantenimientoViewModel model)
        {
            var equipo = await _context.Equipos
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId);

            if (equipo == null) return;

            var tipoEventoMantenimiento = await ObtenerTipoEventoMantenimientoAsync();

            if (tipoEventoMantenimiento == null) return;

            var plantilla = await _context.PlantillasEvento
                .AsNoTracking()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p =>
                    p.TipoEquipoId == equipo.TipoEquipoId &&
                    p.TipoEventoMetrologicoId == tipoEventoMantenimiento.TipoEventoMetrologicoId &&
                    p.Activo);

            if (plantilla == null) return;

            model.Actividades = plantilla.Items
                .Where(i => i.Activo)
                .OrderBy(i => i.Orden)
                .Select(i => new MantenimientoActividadViewModel
                {
                    DescripcionActividad = i.Descripcion,
                    Orden = i.Orden
                })
                .ToList();
        }

        private async Task<EventoMantenimientoDato?> CargarMantenimientoCompletoAsync(int eventoMantenimientoDatoId)
        {
            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                        .ThenInclude(eq => eq.CaracteristicasMetrologicas)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == eventoMantenimientoDatoId);

            if (mantenimiento == null) return null;

            mantenimiento.EventoMetrologico.ActividadesMantenimiento =
                mantenimiento.EventoMetrologico.ActividadesMantenimiento
                    .OrderBy(a => a.Orden)
                    .ToList();

            mantenimiento.EventoMetrologico.Evidencias =
                mantenimiento.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return mantenimiento;
        }

        private async Task<List<string>> DetectarCambiosCriticosAsync(
            EventoMetrologico evento,
            MantenimientoViewModel model)
        {
            var cambios = new List<string>();

            if (evento.FechaEvento.Date != model.FechaEvento.Date)
            {
                cambios.Add(
                    $"Fecha de mantenimiento: {FormatearFecha(evento.FechaEvento)} -> {FormatearFecha(model.FechaEvento)}.");
            }

            if (!SonFechasEquivalentes(evento.FechaProxima, model.FechaProxima))
            {
                cambios.Add(
                    $"Proxima fecha de mantenimiento: {FormatearFecha(evento.FechaProxima)} -> {FormatearFecha(model.FechaProxima)}.");
            }

            if (!SonTextosEquivalentes(evento.EstadoEquipoResultado, model.EstadoEquipoResultado))
            {
                cambios.Add(
                    $"Estado del equipo: {FormatearTexto(evento.EstadoEquipoResultado)} -> {FormatearTexto(model.EstadoEquipoResultado)}.");
            }

            if (evento.SubtipoEventoId != model.SubtipoEventoId)
            {
                var nombresSubtipos = await _context.SubtiposEvento
                    .AsNoTracking()
                    .Where(s =>
                        s.SubtipoEventoId == evento.SubtipoEventoId ||
                        s.SubtipoEventoId == model.SubtipoEventoId)
                    .ToDictionaryAsync(s => s.SubtipoEventoId, s => s.Nombre);

                cambios.Add(
                    $"Subtipo de evento: {ObtenerNombreSubtipo(nombresSubtipos, evento.SubtipoEventoId)} -> {ObtenerNombreSubtipo(nombresSubtipos, model.SubtipoEventoId)}.");
            }

            if (evento.EsHistorico != model.EsHistorico)
            {
                cambios.Add(
                    $"Clasificacion historica: {FormatearClasificacionHistorica(evento.EsHistorico)} -> {FormatearClasificacionHistorica(model.EsHistorico)}.");
            }

            return cambios;
        }

        private async Task RegistrarEdicionCriticaAsync(
            EventoMantenimientoDato mantenimiento,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            var usuarioActual = await _userManager.GetUserAsync(User);
            var rolUsuario = ObtenerRolUsuarioActual();
            var evento = mantenimiento.EventoMetrologico;
            var detalle = string.Join(" ", cambiosCriticos);
            var usuarioResponsable = usuarioActual == null
                ? User.Identity?.Name
                : $"{usuarioActual.NombreCompleto} ({usuarioActual.Email})";

            await _auditoriaMetrologicaService.RegistrarAsync(new AuditoriaMetrologicaRegistro
            {
                UsuarioId = usuarioActual?.Id,
                UsuarioNombre = usuarioActual?.NombreCompleto ?? User.Identity?.Name,
                UsuarioCorreo = usuarioActual?.Email,
                RolUsuario = rolUsuario,
                Accion = "Edicion critica de mantenimiento",
                Entidad = "Mantenimiento",
                EntidadId = mantenimiento.EventoMantenimientoDatoId.ToString(),
                EquipoId = evento.EquipoId,
                CodigoEquipo = evento.Equipo.Codigo,
                NombreEquipo = evento.Equipo.Nombre,
                EventoMetrologicoId = evento.EventoMetrologicoId,
                TipoEvento = "Mantenimiento",
                Detalle = detalle,
                EsCritico = true
            });

            if (User.IsInRole(RolesSistema.Tecnico) && !EsAdministrador())
            {
                await _notificacionMetrologicaService.NotificarEdicionCriticaMantenimientoAsync(
                    mantenimiento.EventoMantenimientoDatoId,
                    usuarioResponsable,
                    cambiosCriticos);
            }
        }

        private async Task RegistrarRegistroHistoricoAsync(EventoMantenimientoDato mantenimiento)
        {
            var evento = mantenimiento.EventoMetrologico;
            await RegistrarAuditoriaHistoricaAsync(
                mantenimiento,
                "Registro historico de mantenimiento",
                $"Se registro un mantenimiento historico con fecha real {FormatearFecha(evento.FechaEvento)}.",
                false);
        }

        private async Task RegistrarCorreccionHistoricaAsync(EventoMantenimientoDato mantenimiento)
        {
            await RegistrarAuditoriaHistoricaAsync(
                mantenimiento,
                "Correccion de mantenimiento historico",
                "Se actualizo un mantenimiento historico durante una correccion administrativa.",
                false);
        }

        private async Task RegistrarAuditoriaHistoricaAsync(
            EventoMantenimientoDato mantenimiento,
            string accion,
            string detalle,
            bool esCritico)
        {
            var usuarioActual = await _userManager.GetUserAsync(User);
            var evento = mantenimiento.EventoMetrologico;

            await _auditoriaMetrologicaService.RegistrarAsync(new AuditoriaMetrologicaRegistro
            {
                UsuarioId = usuarioActual?.Id,
                UsuarioNombre = usuarioActual?.NombreCompleto ?? User.Identity?.Name,
                UsuarioCorreo = usuarioActual?.Email,
                RolUsuario = ObtenerRolUsuarioActual(),
                Accion = accion,
                Entidad = "Mantenimiento",
                EntidadId = mantenimiento.EventoMantenimientoDatoId.ToString(),
                EquipoId = evento.EquipoId,
                CodigoEquipo = evento.Equipo.Codigo,
                NombreEquipo = evento.Equipo.Nombre,
                EventoMetrologicoId = evento.EventoMetrologicoId,
                TipoEvento = "Mantenimiento",
                Detalle = detalle,
                EsCritico = esCritico
            });
        }

        private string ObtenerRolUsuarioActual()
        {
            if (EsAdministrador())
            {
                return RolesSistema.ObtenerNombreVisible(RolesSistema.AdministradorMetrologico);
            }

            if (User.IsInRole(RolesSistema.Tecnico))
            {
                return RolesSistema.ObtenerNombreVisible(RolesSistema.Tecnico);
            }

            return "Usuario";
        }

        private bool EsAdministrador()
        {
            return User.IsInRole(RolesSistema.AdministradorMetrologico);
        }

        private static bool SonFechasEquivalentes(DateTime? fechaAnterior, DateTime? fechaNueva)
        {
            return fechaAnterior?.Date == fechaNueva?.Date;
        }

        private static bool SonTextosEquivalentes(string? textoAnterior, string? textoNuevo)
        {
            return string.Equals(
                NormalizarTexto(textoAnterior),
                NormalizarTexto(textoNuevo),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizarTexto(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? null
                : texto.Trim();
        }

        private static string FormatearFecha(DateTime? fecha)
        {
            return fecha.HasValue
                ? fecha.Value.ToString("yyyy-MM-dd")
                : "No definida";
        }

        private static string FormatearTexto(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? "No definido"
                : texto.Trim();
        }

        private static string ObtenerNombreSubtipo(
            IReadOnlyDictionary<int, string> nombresSubtipos,
            int subtipoEventoId)
        {
            return nombresSubtipos.TryGetValue(subtipoEventoId, out var nombre)
                ? nombre
                : $"Subtipo {subtipoEventoId}";
        }

        private static string FormatearClasificacionHistorica(bool esHistorico)
        {
            return esHistorico
                ? "Historico"
                : "Operativo";
        }

        private async Task<TipoEventoMetrologico?> ObtenerTipoEventoMantenimientoAsync()
        {
            return await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Mantenimiento");
        }

        private async Task ValidarFormularioMantenimientoAsync(
            MantenimientoViewModel model,
            TipoEventoMetrologico? tipoEventoMantenimiento)
        {
            if (!EsAdministrador() &&
                (model.EsHistorico || !string.IsNullOrWhiteSpace(model.ObservacionCargaHistorica)))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Solo un administrador puede registrar o corregir mantenimientos históricos.");
            }

            if (model.EsHistorico && model.FechaEvento.Date >= DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.FechaEvento),
                    "Un mantenimiento histórico debe tener una fecha anterior al día actual.");
            }

            if (!model.Actividades.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos una actividad de mantenimiento.");
            }

            if (tipoEventoMantenimiento == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Mantenimiento'.");
                return;
            }

            await ValidarFechaHistoricaMantenimientoAsync(
                model,
                tipoEventoMantenimiento.TipoEventoMetrologicoId);

            if (!ValidarEvidencias(model.Evidencias))
            {
                ModelState.AddModelError(string.Empty, "Solo se permiten evidencias visuales en formato imagen.");
            }

            if (!ValidarEvidenciasItems(model.Actividades.Select(a => a.EvidenciaImagen)))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Las evidencias por actividad deben ser imágenes.");
            }

            var resultadoRegla = await _metrologyRulesService.EvaluarEventoAsync(
                model.EquipoId,
                tipoEventoMantenimiento.TipoEventoMetrologicoId,
                model.FechaEvento.Date,
                model.SubtipoEventoId,
                model.JustificacionExtraordinario,
                model.EsHistorico);

            model.EsExtraordinario = resultadoRegla.EsExtraordinario;
            model.FechaProxima = resultadoRegla.FechaProximaCalculada;

            ModelState.Remove(nameof(model.FechaProxima));
            ModelState.Remove(nameof(model.EsExtraordinario));

            if (!resultadoRegla.EsValido)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultadoRegla.Mensaje ?? "El evento no cumple las reglas metrológicas.");
            }

            if (!string.IsNullOrWhiteSpace(resultadoRegla.Advertencia))
            {
                TempData["AdvertenciaRegla"] = resultadoRegla.Advertencia;
            }
        }

        private async Task ValidarFechaHistoricaMantenimientoAsync(
            MantenimientoViewModel model,
            int tipoEventoMetrologicoId)
        {
            if (!model.EsHistorico)
            {
                return;
            }

            var ultimaFechaRegistrada = await _context.EventosMetrologicos
                .AsNoTracking()
                .Where(e =>
                    e.Activo &&
                    e.EquipoId == model.EquipoId &&
                    e.TipoEventoMetrologicoId == tipoEventoMetrologicoId &&
                    e.EventoMetrologicoId != model.EventoMetrologicoId)
                .MaxAsync(e => (DateTime?)e.FechaEvento);

            if (!ultimaFechaRegistrada.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.FechaEvento),
                    "No se puede marcar como histórico porque no existe un mantenimiento registrado posterior que sirva como referencia.");
                return;
            }

            if (model.FechaEvento.Date >= ultimaFechaRegistrada.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(model.FechaEvento),
                    $"Un mantenimiento histórico debe tener una fecha anterior al último mantenimiento registrado del equipo ({ultimaFechaRegistrada.Value:yyyy-MM-dd}).");
            }
        }

        private static void NormalizarActividades(MantenimientoViewModel model)
        {
            model.Actividades = model.Actividades
                .Where(a => !string.IsNullOrWhiteSpace(a.DescripcionActividad))
                .ToList();
        }

        private async Task AgregarActividadesAlEventoAsync(
            int eventoMetrologicoId,
            string codigoEquipo,
            List<MantenimientoActividadViewModel> actividades)
        {
            var orden = 1;

            foreach (var actividad in actividades)
            {
                var actividadEvento = new EventoMantenimientoActividad
                {
                    EventoMetrologicoId = eventoMetrologicoId,
                    DescripcionActividad = actividad.DescripcionActividad,
                    Observaciones = actividad.Observaciones,
                    Orden = orden
                };

                await CargarEvidenciaActividadAsync(
                    actividadEvento,
                    codigoEquipo,
                    actividad.EvidenciaImagen);

                _context.EventosMantenimientoActividad.Add(actividadEvento);

                orden++;
            }
        }

        private async Task ReemplazarActividadesAsync(
            EventoMetrologico evento,
            string codigoEquipo,
            List<MantenimientoActividadViewModel> actividades)
        {
            var actividadesExistentes = evento.ActividadesMantenimiento
                .ToDictionary(a => a.EventoMantenimientoActividadId);

            _context.EventosMantenimientoActividad.RemoveRange(evento.ActividadesMantenimiento);

            var orden = 1;
            foreach (var actividad in actividades)
            {
                actividadesExistentes.TryGetValue(
                    actividad.EventoMantenimientoActividadId,
                    out var actividadExistente);

                var actividadEvento = new EventoMantenimientoActividad
                {
                    EventoMetrologicoId = evento.EventoMetrologicoId,
                    DescripcionActividad = actividad.DescripcionActividad,
                    Observaciones = actividad.Observaciones,
                    EvidenciaNombreArchivo = actividadExistente?.EvidenciaNombreArchivo,
                    EvidenciaContentType = actividadExistente?.EvidenciaContentType,
                    EvidenciaGoogleDriveFileId = actividadExistente?.EvidenciaGoogleDriveFileId,
                    EvidenciaRutaArchivo = actividadExistente?.EvidenciaRutaArchivo,
                    Orden = orden
                };

                await CargarEvidenciaActividadAsync(
                    actividadEvento,
                    codigoEquipo,
                    actividad.EvidenciaImagen);

                _context.EventosMantenimientoActividad.Add(actividadEvento);
                orden++;
            }
        }

        private static bool ValidarEvidencias(List<IFormFile> evidencias)
        {
            if (evidencias == null || !evidencias.Any())
                return true;

            return evidencias
                .Where(e => e != null && e.Length > 0)
                .All(e =>
                    !string.IsNullOrWhiteSpace(e.ContentType) &&
                    e.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ValidarEvidenciasItems(IEnumerable<IFormFile?> evidencias)
        {
            return evidencias
                .Where(e => e != null && e.Length > 0)
                .All(e =>
                    !string.IsNullOrWhiteSpace(e!.ContentType) &&
                    e.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        }

        private async Task CargarEvidenciaActividadAsync(
            EventoMantenimientoActividad actividad,
            string codigoEquipo,
            IFormFile? evidencia)
        {
            if (evidencia == null ||
                evidencia.Length == 0 ||
                string.IsNullOrWhiteSpace(evidencia.ContentType) ||
                !evidencia.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                codigoEquipo,
                "Evidencias",
                "Mantenimientos",
                "Items");

            await using var stream = evidencia.OpenReadStream();

            var extension = Path.GetExtension(evidencia.FileName);
            var nombreLimpio = Path.GetFileNameWithoutExtension(evidencia.FileName);
            var nombreArchivoDrive =
                $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{nombreLimpio}{extension}";

            var uploadResult = await _googleDriveService.UploadFileAsync(
                stream,
                nombreArchivoDrive,
                evidencia.ContentType,
                folderId);

            actividad.EvidenciaNombreArchivo = evidencia.FileName;
            actividad.EvidenciaContentType = evidencia.ContentType;
            actividad.EvidenciaGoogleDriveFileId = uploadResult.FileId;
            actividad.EvidenciaRutaArchivo = uploadResult.WebViewLink;
        }

        private async Task<List<EvidenciaEventoMetrologico>> SubirEvidenciasAsync(
            int eventoMetrologicoId,
            string codigoEquipo,
            List<IFormFile> evidencias)
        {
            var evidenciasGuardadas = new List<EvidenciaEventoMetrologico>();

            if (evidencias == null || evidencias.Count == 0)
                return evidenciasGuardadas;

            var evidenciasValidas = evidencias
                .Where(e =>
                    e != null &&
                    e.Length > 0 &&
                    !string.IsNullOrWhiteSpace(e.ContentType) &&
                    e.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!evidenciasValidas.Any())
                return evidenciasGuardadas;

            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                codigoEquipo,
                "Evidencias",
                "Mantenimientos");

            foreach (var evidencia in evidenciasValidas)
            {
                await using var stream = evidencia.OpenReadStream();

                var extension = Path.GetExtension(evidencia.FileName);
                var nombreLimpio = Path.GetFileNameWithoutExtension(evidencia.FileName);
                var nombreArchivoDrive =
                    $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{nombreLimpio}{extension}";

                var uploadResult = await _googleDriveService.UploadFileAsync(
                    stream,
                    nombreArchivoDrive,
                    evidencia.ContentType,
                    folderId);

                evidenciasGuardadas.Add(new EvidenciaEventoMetrologico
                {
                    EventoMetrologicoId = eventoMetrologicoId,
                    NombreArchivo = evidencia.FileName,
                    ContentType = evidencia.ContentType,
                    GoogleDriveFileId = uploadResult.FileId,
                    RutaArchivo = uploadResult.WebViewLink,
                    TipoEvidencia = "Imagen",
                    Descripcion = null,
                    FechaCarga = DateTime.UtcNow,
                    Activo = true
                });
            }

            return evidenciasGuardadas;
        }

        private async Task GenerarYSubirPdfAsync(EventoMantenimientoDato mantenimiento)
        {
            var pdfBytes = _mantenimientoPdfService.Generar(mantenimiento);

            var codigoEquipo = mantenimiento.EventoMetrologico.Equipo.Codigo;
            var fecha = mantenimiento.EventoMetrologico.FechaEvento.ToString("yyyy-MM-dd");
            var nombreArchivo = $"Mantenimiento-{codigoEquipo}-{fecha}.pdf";

            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                codigoEquipo,
                "Documentos",
                "Mantenimientos");

            using var pdfStream = new MemoryStream(pdfBytes);

            var uploadResult = await _googleDriveService.UploadFileAsync(
                pdfStream,
                nombreArchivo,
                "application/pdf",
                folderId);

            mantenimiento.GoogleDriveFileId = uploadResult.FileId;
            mantenimiento.NombreArchivoPdf = uploadResult.FileName;
            mantenimiento.RutaPdf = uploadResult.WebViewLink;
        }

        private async Task CargarEvidenciasExistentesEnModeloAsync(MantenimientoViewModel model)
        {
            if (model.EventoMetrologicoId <= 0)
                return;

            model.EvidenciasExistentes = await _context.EvidenciasEventoMetrologico
                .AsNoTracking()
                .Where(e => e.EventoMetrologicoId == model.EventoMetrologicoId && e.Activo)
                .OrderByDescending(e => e.FechaCarga)
                .Select(e => new EvidenciaEventoViewModel
                {
                    EvidenciaEventoMetrologicoId = e.EvidenciaEventoMetrologicoId,
                    NombreArchivo = e.NombreArchivo,
                    ContentType = e.ContentType,
                    GoogleDriveFileId = e.GoogleDriveFileId,
                    RutaArchivo = e.RutaArchivo,
                    TipoEvidencia = e.TipoEvidencia,
                    Descripcion = e.Descripcion,
                    FechaCarga = e.FechaCarga,
                    Activo = e.Activo
                })
                .ToListAsync();
        }
    }
}
