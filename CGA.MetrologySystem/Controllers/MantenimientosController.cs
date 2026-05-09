using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize]
    public class MantenimientosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly MantenimientoPdfService _mantenimientoPdfService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IMetrologyRulesService _metrologyRulesService;

        public MantenimientosController(
            AppDbContext context,
            MantenimientoPdfService mantenimientoPdfService,
            IGoogleDriveService googleDriveService,
            IMetrologyRulesService metrologyRulesService)
        {
            _context = context;
            _mantenimientoPdfService = mantenimientoPdfService;
            _googleDriveService = googleDriveService;
            _metrologyRulesService = metrologyRulesService;
        }

        public async Task<IActionResult> Index()
        {
            var mantenimientos = await _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(m => m.TipoMantenimiento)
                .OrderByDescending(m => m.EventoMetrologico.FechaEvento)
                .ToListAsync();

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

                AgregarActividadesAlEvento(eventoMetrologico.EventoMetrologicoId, model.Actividades);

                await _context.SaveChangesAsync();

                var mantenimientoCompleto = await CargarMantenimientoCompletoAsync(
                    mantenimientoDato.EventoMantenimientoDatoId);

                if (mantenimientoCompleto == null)
                    throw new Exception("No se pudo recuperar el mantenimiento para generar el PDF.");

                await GenerarYSubirPdfAsync(mantenimientoCompleto);

                var codigoEquipo = mantenimientoCompleto.EventoMetrologico.Equipo.Codigo;

                var evidencias = await SubirEvidenciasAsync(
                    eventoMetrologico.EventoMetrologicoId,
                    codigoEquipo,
                    model.Evidencias);

                if (evidencias.Any())
                    _context.EvidenciasEventoMetrologico.AddRange(evidencias);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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
                EsExtraordinario = evento.EsExtraordinario,
                JustificacionExtraordinario = evento.JustificacionExtraordinario,
                Actividades = evento.ActividadesMantenimiento
                    .OrderBy(a => a.Orden)
                    .Select(a => new MantenimientoActividadViewModel
                    {
                        EventoMantenimientoActividadId = a.EventoMantenimientoActividadId,
                        DescripcionActividad = a.DescripcionActividad,
                        Observaciones = a.Observaciones,
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
                        .ThenInclude(e => e.Evidencias)
                    .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

                if (mantenimiento == null)
                    return NotFound();

                var evento = mantenimiento.EventoMetrologico;

                evento.EquipoId = model.EquipoId;
                evento.TipoEventoMetrologicoId = tipoEventoMantenimiento!.TipoEventoMetrologicoId;
                evento.SubtipoEventoId = model.SubtipoEventoId;
                evento.ResponsableInternoId = model.ResponsableInternoId;
                evento.FechaEvento = model.FechaEvento.Date;
                evento.FechaProxima = model.FechaProxima;
                evento.EstadoEquipoResultado = model.EstadoEquipoResultado;
                evento.ComentariosAdicionales = model.ComentariosAdicionales;
                evento.EsExtraordinario = model.EsExtraordinario;
                evento.JustificacionExtraordinario = model.JustificacionExtraordinario;

                mantenimiento.TipoMantenimientoId = model.TipoMantenimientoId;

                _context.EventosMantenimientoActividad.RemoveRange(evento.ActividadesMantenimiento);
                AgregarActividadesAlEvento(evento.EventoMetrologicoId, model.Actividades);

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

            mantenimiento.EventoMetrologico.Evidencias =
                mantenimiento.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return View(mantenimiento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento)
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

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

        private async Task<TipoEventoMetrologico?> ObtenerTipoEventoMantenimientoAsync()
        {
            return await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Mantenimiento");
        }

        private async Task ValidarFormularioMantenimientoAsync(
            MantenimientoViewModel model,
            TipoEventoMetrologico? tipoEventoMantenimiento)
        {
            if (!model.Actividades.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos una actividad de mantenimiento.");
            }

            if (tipoEventoMantenimiento == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Mantenimiento'.");
                return;
            }

            if (!ValidarEvidencias(model.Evidencias))
            {
                ModelState.AddModelError(string.Empty, "Solo se permiten evidencias visuales en formato imagen.");
            }

            var resultadoRegla = await _metrologyRulesService.EvaluarEventoAsync(
                model.EquipoId,
                tipoEventoMantenimiento.TipoEventoMetrologicoId,
                model.FechaEvento.Date,
                model.JustificacionExtraordinario);

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

        private static void NormalizarActividades(MantenimientoViewModel model)
        {
            model.Actividades = model.Actividades
                .Where(a => !string.IsNullOrWhiteSpace(a.DescripcionActividad))
                .ToList();
        }

        private void AgregarActividadesAlEvento(
            int eventoMetrologicoId,
            List<MantenimientoActividadViewModel> actividades)
        {
            var orden = 1;

            foreach (var actividad in actividades)
            {
                _context.EventosMantenimientoActividad.Add(new EventoMantenimientoActividad
                {
                    EventoMetrologicoId = eventoMetrologicoId,
                    DescripcionActividad = actividad.DescripcionActividad,
                    Observaciones = actividad.Observaciones,
                    Orden = orden
                });

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