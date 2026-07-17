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
    public class VerificacionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly VerificacionPdfService _verificacionPdfService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IMetrologyRulesService _metrologyRulesService;
        private readonly INotificacionMetrologicaService _notificacionMetrologicaService;
        private readonly IAuditoriaMetrologicaService _auditoriaMetrologicaService;
        private readonly UserManager<Infrastructure.Identity.UsuarioSistema> _userManager;
        private readonly ILogger<VerificacionesController> _logger;

        public VerificacionesController(
            AppDbContext context,
            VerificacionPdfService verificacionPdfService,
            IGoogleDriveService googleDriveService,
            IMetrologyRulesService metrologyRulesService,
            INotificacionMetrologicaService notificacionMetrologicaService,
            IAuditoriaMetrologicaService auditoriaMetrologicaService,
            UserManager<Infrastructure.Identity.UsuarioSistema> userManager,
            ILogger<VerificacionesController> logger)
        {
            _context = context;
            _verificacionPdfService = verificacionPdfService;
            _googleDriveService = googleDriveService;
            _metrologyRulesService = metrologyRulesService;
            _notificacionMetrologicaService = notificacionMetrologicaService;
            _auditoriaMetrologicaService = auditoriaMetrologicaService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? buscar,
            string? estado,
            int? subtipoEventoId,
            string? clasificacion,
            DateTime? desde,
            DateTime? hasta)
        {
            var query = _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                query = query.Where(v =>
                    v.EventoMetrologico.Equipo.Codigo.Contains(texto) ||
                    v.EventoMetrologico.Equipo.Nombre.Contains(texto) ||
                    (v.EventoMetrologico.ResponsableInterno.NombreCompleto.Contains(texto)));
            }

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(v => v.EventoMetrologico.EstadoEquipoResultado == estado);

            if (subtipoEventoId.HasValue)
                query = query.Where(v => v.EventoMetrologico.SubtipoEventoId == subtipoEventoId.Value);

            if (desde.HasValue)
                query = query.Where(v => v.EventoMetrologico.FechaEvento >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(v => v.EventoMetrologico.FechaEvento <= hasta.Value.Date);

            query = clasificacion switch
            {
                "historicas" => query.Where(v => v.EventoMetrologico.EsHistorico),
                "extraordinarias" => query.Where(v => v.EventoMetrologico.EsExtraordinario),
                "operativas" => query.Where(v => !v.EventoMetrologico.EsHistorico),
                _ => query
            };

            var verificaciones = await query
                .OrderByDescending(v => v.EventoMetrologico.FechaEvento)
                .ToListAsync();

            ViewBag.Buscar = buscar;
            ViewBag.Estado = estado;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.SubtiposEvento = await CrearOpcionesSubtiposAsync(subtipoEventoId);
            ViewBag.Estados = CrearOpcionesEstado(estado);
            ViewBag.Clasificaciones = CrearOpcionesClasificacion(clasificacion);
            return View(verificaciones);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion.OrderBy(r => r.Orden))
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            verificacion.EventoMetrologico.Evidencias =
                verificacion.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return View(verificacion);
        }

        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Create(int? equipoId = null)
        {
            var model = new VerificacionViewModel
            {
                FechaEvento = DateTime.Today,
                EquipoId = equipoId ?? 0,
                EstadoEquipoResultado = "Operativo"
            };

            await CargarCombosAsync(model);

            if (equipoId.HasValue && equipoId.Value > 0)
            {
                await CargarResultadosDesdePlantillaAsync(model);
            }

            if (!model.Resultados.Any())
            {
                model.Resultados.Add(new VerificacionResultadoViewModel
                {
                    Orden = 1,
                    Cumple = true
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Create(VerificacionViewModel model)
        {
            NormalizarResultados(model);

            var tipoEventoVerificacion = await ObtenerTipoEventoVerificacionAsync();

            await ValidarFormularioVerificacionAsync(model, tipoEventoVerificacion);

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var etapaGuardado = "crear el evento metrologico";

            try
            {
                var eventoMetrologico = new EventoMetrologico
                {
                    EquipoId = model.EquipoId,
                    TipoEventoMetrologicoId = tipoEventoVerificacion!.TipoEventoMetrologicoId,
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

                etapaGuardado = "crear el detalle de verificacion";
                var verificacionDato = new EventoVerificacionDato
                {
                    EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId
                };

                _context.EventosVerificacionDato.Add(verificacionDato);
                await _context.SaveChangesAsync();

                var codigoEquipo = await _context.Equipos
                    .Where(e => e.EquipoId == model.EquipoId)
                    .Select(e => e.Codigo)
                    .FirstAsync();

                etapaGuardado = "guardar los resultados de verificacion";
                await AgregarResultadosAlEventoAsync(
                    eventoMetrologico.EventoMetrologicoId,
                    codigoEquipo,
                    model.Resultados);

                await _context.SaveChangesAsync();

                etapaGuardado = "recuperar la verificacion para generar el PDF";
                var verificacionCompleta = await CargarVerificacionCompletaAsync(
                    verificacionDato.EventoVerificacionDatoId);

                if (verificacionCompleta == null)
                    throw new Exception("No se pudo recuperar la verificación para generar el PDF.");

                etapaGuardado = "generar el PDF de verificacion";
                var pdfBytes = _verificacionPdfService.Generar(verificacionCompleta);

                etapaGuardado = "subir el PDF de verificacion a Google Drive";
                await SubirPdfAsync(verificacionCompleta, pdfBytes);

                etapaGuardado = "subir evidencias de verificacion";
                var evidencias = await SubirEvidenciasAsync(
                    eventoMetrologico.EventoMetrologicoId,
                    codigoEquipo,
                    model.Evidencias);

                if (evidencias.Any())
                    _context.EvidenciasEventoMetrologico.AddRange(evidencias);

                etapaGuardado = "confirmar la verificacion";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (eventoMetrologico.EsHistorico)
                {
                    await RegistrarRegistroHistoricoAsync(verificacionCompleta);
                }

                await _notificacionMetrologicaService.NotificarEventoExtraordinarioAsync(
                    eventoMetrologico.EventoMetrologicoId);

                return RedirectToAction(nameof(Details), new { id = verificacionDato.EventoVerificacionDatoId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Fallo el registro de verificacion extraordinaria={EsExtraordinario} para equipo {EquipoId} al {EtapaGuardado}.",
                    model.EsExtraordinario,
                    model.EquipoId,
                    etapaGuardado);

                ModelState.AddModelError(
                    string.Empty,
                    $"Ocurrió un error al guardar la verificación durante: {etapaGuardado}.");
                await CargarCombosAsync(model);
                return View(model);
            }
        }

        [Authorize(Roles = RolesSistema.OperacionMetrologica)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion.OrderBy(r => r.Orden))
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            var evento = verificacion.EventoMetrologico;

            if (!evento.Activo)
                return Forbid();

            if (evento.EsHistorico && !EsAdministrador())
                return Forbid();

            var model = new VerificacionViewModel
            {
                EventoMetrologicoId = evento.EventoMetrologicoId,
                EventoVerificacionDatoId = verificacion.EventoVerificacionDatoId,
                EquipoId = evento.EquipoId,
                SubtipoEventoId = evento.SubtipoEventoId,
                ResponsableInternoId = evento.ResponsableInternoId,
                FechaEvento = evento.FechaEvento,
                FechaProxima = evento.FechaProxima,
                EstadoEquipoResultado = evento.EstadoEquipoResultado,
                ComentariosAdicionales = evento.ComentariosAdicionales,
                EsHistorico = evento.EsHistorico,
                ObservacionCargaHistorica = evento.ObservacionCargaHistorica,
                EsExtraordinario = evento.EsExtraordinario,
                JustificacionExtraordinario = evento.JustificacionExtraordinario,
                Resultados = evento.ResultadosVerificacion
                    .OrderBy(r => r.Orden)
                    .Select(r => new VerificacionResultadoViewModel
                    {
                        EventoVerificacionResultadoId = r.EventoVerificacionResultadoId,
                        DescripcionItem = r.DescripcionItem,
                        Cumple = r.Cumple,
                        Observaciones = r.Observaciones,
                        EvidenciaNombreArchivo = r.EvidenciaNombreArchivo,
                        EvidenciaRutaArchivo = r.EvidenciaRutaArchivo,
                        Orden = r.Orden
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
        public async Task<IActionResult> Edit(int id, VerificacionViewModel model)
        {
            if (id != model.EventoVerificacionDatoId)
                return NotFound();

            NormalizarResultados(model);

            var tipoEventoVerificacion = await ObtenerTipoEventoVerificacionAsync();

            await ValidarFormularioVerificacionAsync(model, tipoEventoVerificacion);

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                await CargarEvidenciasExistentesEnModeloAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var verificacion = await _context.EventosVerificacionDato
                    .Include(v => v.EventoMetrologico)
                        .ThenInclude(e => e.ResultadosVerificacion)
                    .Include(v => v.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                    .Include(v => v.EventoMetrologico)
                        .ThenInclude(e => e.Evidencias)
                    .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

                if (verificacion == null)
                    return NotFound();

                var evento = verificacion.EventoMetrologico;

                if (!evento.Activo)
                    return Forbid();

                if (evento.EsHistorico && !EsAdministrador())
                    return Forbid();

                var eraHistorico = evento.EsHistorico;
                var cambiosCriticos = await DetectarCambiosCriticosAsync(evento, model);

                evento.EquipoId = model.EquipoId;
                evento.TipoEventoMetrologicoId = tipoEventoVerificacion!.TipoEventoMetrologicoId;
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

                await ReemplazarResultadosAsync(
                    evento,
                    evento.Equipo.Codigo,
                    model.Resultados);

                await _context.SaveChangesAsync();

                var verificacionCompleta = await CargarVerificacionCompletaAsync(
                    verificacion.EventoVerificacionDatoId);

                if (verificacionCompleta == null)
                    throw new Exception("No se pudo recuperar la verificación para regenerar el PDF.");

                if (!string.IsNullOrWhiteSpace(verificacion.GoogleDriveFileId))
                    await _googleDriveService.DeleteFileAsync(verificacion.GoogleDriveFileId);

                var pdfBytes = _verificacionPdfService.Generar(verificacionCompleta);
                await SubirPdfAsync(verificacionCompleta, pdfBytes);

                var codigoEquipo = verificacionCompleta.EventoMetrologico.Equipo.Codigo;

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
                        verificacionCompleta,
                        cambiosCriticos);
                }
                else if (EsAdministrador() && (eraHistorico || model.EsHistorico))
                {
                    await RegistrarCorreccionHistoricaAsync(verificacionCompleta);
                }

                return RedirectToAction(nameof(Details), new { id = verificacion.EventoVerificacionDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar la verificación.");
                await CargarCombosAsync(model);
                await CargarEvidenciasExistentesEnModeloAsync(model);
                return View(model);
            }
        }

        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion.OrderBy(r => r.Orden))
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            if (!verificacion.EventoMetrologico.Activo)
                return Forbid();

            if (verificacion.EventoMetrologico.EsHistorico && !EsAdministrador())
                return Forbid();

            verificacion.EventoMetrologico.Evidencias =
                verificacion.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return View(verificacion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            if (!verificacion.EventoMetrologico.Activo)
                return Forbid();

            if (verificacion.EventoMetrologico.EsHistorico && !EsAdministrador())
                return Forbid();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var evento = verificacion.EventoMetrologico;

                if (!string.IsNullOrWhiteSpace(verificacion.GoogleDriveFileId))
                    await _googleDriveService.DeleteFileAsync(verificacion.GoogleDriveFileId);

                foreach (var evidencia in evento.Evidencias)
                {
                    if (!string.IsNullOrWhiteSpace(evidencia.GoogleDriveFileId))
                        await _googleDriveService.DeleteFileAsync(evidencia.GoogleDriveFileId);
                }

                _context.EventosVerificacionResultado.RemoveRange(evento.ResultadosVerificacion);
                _context.EvidenciasEventoMetrologico.RemoveRange(evento.Evidencias);
                _context.EventosVerificacionDato.Remove(verificacion);
                _context.EventosMetrologicos.Remove(evento);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] = "Ocurrió un error al eliminar la verificación.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task CargarCombosAsync(VerificacionViewModel model)
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
                new("Operativas", "operativas", clasificacion == "operativas"),
                new("Históricas", "historicas", clasificacion == "historicas"),
                new("Extraordinarias", "extraordinarias", clasificacion == "extraordinarias")
            };
        }

        private async Task CargarResultadosDesdePlantillaAsync(VerificacionViewModel model)
        {
            var equipo = await _context.Equipos
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId);

            if (equipo == null) return;

            var tipoEventoVerificacion = await ObtenerTipoEventoVerificacionAsync();

            if (tipoEventoVerificacion == null) return;

            var plantilla = await _context.PlantillasEvento
                .AsNoTracking()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p =>
                    p.TipoEquipoId == equipo.TipoEquipoId &&
                    p.TipoEventoMetrologicoId == tipoEventoVerificacion.TipoEventoMetrologicoId &&
                    p.Activo);

            if (plantilla == null) return;

            model.Resultados = plantilla.Items
                .Where(i => i.Activo)
                .OrderBy(i => i.Orden)
                .Select(i => new VerificacionResultadoViewModel
                {
                    DescripcionItem = i.Descripcion,
                    Cumple = true,
                    Orden = i.Orden
                })
                .ToList();
        }

        private async Task<EventoVerificacionDato?> CargarVerificacionCompletaAsync(int eventoVerificacionDatoId)
        {
            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                        .ThenInclude(eq => eq.CaracteristicasMetrologicas)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == eventoVerificacionDatoId);

            if (verificacion == null) return null;

            verificacion.EventoMetrologico.ResultadosVerificacion =
                verificacion.EventoMetrologico.ResultadosVerificacion
                    .OrderBy(r => r.Orden)
                    .ToList();

            verificacion.EventoMetrologico.Evidencias =
                verificacion.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return verificacion;
        }

        private async Task<List<string>> DetectarCambiosCriticosAsync(
            EventoMetrologico evento,
            VerificacionViewModel model)
        {
            var cambios = new List<string>();

            if (evento.FechaEvento.Date != model.FechaEvento.Date)
            {
                cambios.Add(
                    $"Fecha de verificacion: {FormatearFecha(evento.FechaEvento)} -> {FormatearFecha(model.FechaEvento)}.");
            }

            if (!SonFechasEquivalentes(evento.FechaProxima, model.FechaProxima))
            {
                cambios.Add(
                    $"Proxima fecha de verificacion: {FormatearFecha(evento.FechaProxima)} -> {FormatearFecha(model.FechaProxima)}.");
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
            EventoVerificacionDato verificacion,
            IReadOnlyCollection<string> cambiosCriticos)
        {
            var usuarioActual = await _userManager.GetUserAsync(User);
            var rolUsuario = ObtenerRolUsuarioActual();
            var evento = verificacion.EventoMetrologico;
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
                Accion = "Edicion critica de verificacion",
                Entidad = "Verificacion",
                EntidadId = verificacion.EventoVerificacionDatoId.ToString(),
                EquipoId = evento.EquipoId,
                CodigoEquipo = evento.Equipo.Codigo,
                NombreEquipo = evento.Equipo.Nombre,
                EventoMetrologicoId = evento.EventoMetrologicoId,
                TipoEvento = "Verificacion",
                Detalle = detalle,
                EsCritico = true
            });

            if (User.IsInRole(RolesSistema.Tecnico) && !EsAdministrador())
            {
                await _notificacionMetrologicaService.NotificarEdicionCriticaVerificacionAsync(
                    verificacion.EventoVerificacionDatoId,
                    usuarioResponsable,
                    cambiosCriticos);
            }
        }

        private async Task RegistrarRegistroHistoricoAsync(EventoVerificacionDato verificacion)
        {
            var evento = verificacion.EventoMetrologico;
            await RegistrarAuditoriaHistoricaAsync(
                verificacion,
                "Registro historico de verificacion",
                $"Se registro una verificacion historica con fecha real {FormatearFecha(evento.FechaEvento)}.",
                false);
        }

        private async Task RegistrarCorreccionHistoricaAsync(EventoVerificacionDato verificacion)
        {
            await RegistrarAuditoriaHistoricaAsync(
                verificacion,
                "Correccion de verificacion historica",
                "Se actualizo una verificacion historica durante una correccion administrativa.",
                false);
        }

        private async Task RegistrarAuditoriaHistoricaAsync(
            EventoVerificacionDato verificacion,
            string accion,
            string detalle,
            bool esCritico)
        {
            var usuarioActual = await _userManager.GetUserAsync(User);
            var evento = verificacion.EventoMetrologico;

            await _auditoriaMetrologicaService.RegistrarAsync(new AuditoriaMetrologicaRegistro
            {
                UsuarioId = usuarioActual?.Id,
                UsuarioNombre = usuarioActual?.NombreCompleto ?? User.Identity?.Name,
                UsuarioCorreo = usuarioActual?.Email,
                RolUsuario = ObtenerRolUsuarioActual(),
                Accion = accion,
                Entidad = "Verificacion",
                EntidadId = verificacion.EventoVerificacionDatoId.ToString(),
                EquipoId = evento.EquipoId,
                CodigoEquipo = evento.Equipo.Codigo,
                NombreEquipo = evento.Equipo.Nombre,
                EventoMetrologicoId = evento.EventoMetrologicoId,
                TipoEvento = "Verificacion",
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

        private async Task<TipoEventoMetrologico?> ObtenerTipoEventoVerificacionAsync()
        {
            return await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre.ToLower().Contains("verific"));
        }

        private async Task ValidarFormularioVerificacionAsync(
            VerificacionViewModel model,
            TipoEventoMetrologico? tipoEventoVerificacion)
        {
            if (!EsAdministrador() &&
                (model.EsHistorico || !string.IsNullOrWhiteSpace(model.ObservacionCargaHistorica)))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Solo un administrador puede registrar o corregir verificaciones históricas.");
            }

            if (model.EsHistorico && model.FechaEvento.Date >= DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.FechaEvento),
                    "Una verificación histórica debe tener una fecha anterior al día actual.");
            }

            if (!model.Resultados.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos una condición de verificación.");
            }

            if (tipoEventoVerificacion == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Verificación'.");
                return;
            }

            await ValidarFechaHistoricaVerificacionAsync(
                model,
                tipoEventoVerificacion.TipoEventoMetrologicoId);

            if (!ValidarEvidencias(model.Evidencias))
            {
                ModelState.AddModelError(string.Empty, "Solo se permiten evidencias visuales en formato imagen.");
            }

            if (!ValidarEvidenciasItems(model.Resultados.Select(r => r.EvidenciaImagen)))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Las evidencias por condición deben ser imágenes.");
            }

            var resultadoRegla = await _metrologyRulesService.EvaluarEventoAsync(
                model.EquipoId,
                tipoEventoVerificacion.TipoEventoMetrologicoId,
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

        private async Task ValidarFechaHistoricaVerificacionAsync(
            VerificacionViewModel model,
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
                    "No se puede marcar como histórica porque no existe una verificación registrada posterior que sirva como referencia.");
                return;
            }

            if (model.FechaEvento.Date >= ultimaFechaRegistrada.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(model.FechaEvento),
                    $"Una verificación histórica debe tener una fecha anterior a la última verificación registrada del equipo ({ultimaFechaRegistrada.Value:yyyy-MM-dd}).");
            }
        }

        private static void NormalizarResultados(VerificacionViewModel model)
        {
            model.Resultados = model.Resultados
                .Where(r => !string.IsNullOrWhiteSpace(r.DescripcionItem))
                .ToList();
        }

        private async Task AgregarResultadosAlEventoAsync(
            int eventoMetrologicoId,
            string codigoEquipo,
            List<VerificacionResultadoViewModel> resultados)
        {
            var orden = 1;

            foreach (var resultado in resultados)
            {
                var resultadoEvento = new EventoVerificacionResultado
                {
                    EventoMetrologicoId = eventoMetrologicoId,
                    DescripcionItem = resultado.DescripcionItem,
                    Cumple = resultado.Cumple,
                    Observaciones = resultado.Observaciones,
                    Orden = orden
                };

                await CargarEvidenciaResultadoAsync(
                    resultadoEvento,
                    codigoEquipo,
                    resultado.EvidenciaImagen);

                _context.EventosVerificacionResultado.Add(resultadoEvento);

                orden++;
            }
        }

        private async Task ReemplazarResultadosAsync(
            EventoMetrologico evento,
            string codigoEquipo,
            List<VerificacionResultadoViewModel> resultados)
        {
            var resultadosExistentes = evento.ResultadosVerificacion
                .ToDictionary(r => r.EventoVerificacionResultadoId);

            _context.EventosVerificacionResultado.RemoveRange(evento.ResultadosVerificacion);

            var orden = 1;
            foreach (var resultado in resultados)
            {
                resultadosExistentes.TryGetValue(
                    resultado.EventoVerificacionResultadoId,
                    out var resultadoExistente);

                var resultadoEvento = new EventoVerificacionResultado
                {
                    EventoMetrologicoId = evento.EventoMetrologicoId,
                    DescripcionItem = resultado.DescripcionItem,
                    Cumple = resultado.Cumple,
                    Observaciones = resultado.Observaciones,
                    EvidenciaNombreArchivo = resultadoExistente?.EvidenciaNombreArchivo,
                    EvidenciaContentType = resultadoExistente?.EvidenciaContentType,
                    EvidenciaGoogleDriveFileId = resultadoExistente?.EvidenciaGoogleDriveFileId,
                    EvidenciaRutaArchivo = resultadoExistente?.EvidenciaRutaArchivo,
                    Orden = orden
                };

                await CargarEvidenciaResultadoAsync(
                    resultadoEvento,
                    codigoEquipo,
                    resultado.EvidenciaImagen);

                _context.EventosVerificacionResultado.Add(resultadoEvento);
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

        private async Task CargarEvidenciaResultadoAsync(
            EventoVerificacionResultado resultado,
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
                "Verificaciones",
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

            resultado.EvidenciaNombreArchivo = evidencia.FileName;
            resultado.EvidenciaContentType = evidencia.ContentType;
            resultado.EvidenciaGoogleDriveFileId = uploadResult.FileId;
            resultado.EvidenciaRutaArchivo = uploadResult.WebViewLink;
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
                "Verificaciones");

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

        private async Task SubirPdfAsync(EventoVerificacionDato verificacion, byte[] pdfBytes)
        {
            var codigoEquipo = verificacion.EventoMetrologico.Equipo.Codigo;
            var fecha = verificacion.EventoMetrologico.FechaEvento.ToString("yyyy-MM-dd");
            var nombreArchivo = $"Verificacion-{codigoEquipo}-{fecha}.pdf";

            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                codigoEquipo,
                "Documentos",
                "Verificaciones");

            using var pdfStream = new MemoryStream(pdfBytes);

            var uploadResult = await _googleDriveService.UploadFileAsync(
                pdfStream,
                nombreArchivo,
                "application/pdf",
                folderId);

            verificacion.GoogleDriveFileId = uploadResult.FileId;
            verificacion.NombreArchivoPdf = uploadResult.FileName;
            verificacion.RutaPdf = uploadResult.WebViewLink;
        }

        private async Task CargarEvidenciasExistentesEnModeloAsync(VerificacionViewModel model)
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
