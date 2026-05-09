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
    public class VerificacionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly VerificacionPdfService _verificacionPdfService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IMetrologyRulesService _metrologyRulesService;

        public VerificacionesController(
            AppDbContext context,
            VerificacionPdfService verificacionPdfService,
            IGoogleDriveService googleDriveService,
            IMetrologyRulesService metrologyRulesService)
        {
            _context = context;
            _verificacionPdfService = verificacionPdfService;
            _googleDriveService = googleDriveService;
            _metrologyRulesService = metrologyRulesService;
        }

        public async Task<IActionResult> Index()
        {
            var verificaciones = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .OrderByDescending(v => v.EventoMetrologico.FechaEvento)
                .ToListAsync();

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
                    EsExtraordinario = model.EsExtraordinario,
                    JustificacionExtraordinario = model.JustificacionExtraordinario,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true
                };

                _context.EventosMetrologicos.Add(eventoMetrologico);
                await _context.SaveChangesAsync();

                var verificacionDato = new EventoVerificacionDato
                {
                    EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId
                };

                _context.EventosVerificacionDato.Add(verificacionDato);
                await _context.SaveChangesAsync();

                AgregarResultadosAlEvento(eventoMetrologico.EventoMetrologicoId, model.Resultados);

                await _context.SaveChangesAsync();

                var verificacionCompleta = await CargarVerificacionCompletaAsync(
                    verificacionDato.EventoVerificacionDatoId);

                if (verificacionCompleta == null)
                    throw new Exception("No se pudo recuperar la verificación para generar el PDF.");

                await GenerarYSubirPdfAsync(verificacionCompleta);

                var codigoEquipo = verificacionCompleta.EventoMetrologico.Equipo.Codigo;

                var evidencias = await SubirEvidenciasAsync(
                    eventoMetrologico.EventoMetrologicoId,
                    codigoEquipo,
                    model.Evidencias);

                if (evidencias.Any())
                    _context.EvidenciasEventoMetrologico.AddRange(evidencias);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = verificacionDato.EventoVerificacionDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la verificación.");
                await CargarCombosAsync(model);
                return View(model);
            }
        }

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
                        .ThenInclude(e => e.Evidencias)
                    .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

                if (verificacion == null)
                    return NotFound();

                var evento = verificacion.EventoMetrologico;

                evento.EquipoId = model.EquipoId;
                evento.TipoEventoMetrologicoId = tipoEventoVerificacion!.TipoEventoMetrologicoId;
                evento.SubtipoEventoId = model.SubtipoEventoId;
                evento.ResponsableInternoId = model.ResponsableInternoId;
                evento.FechaEvento = model.FechaEvento.Date;
                evento.FechaProxima = model.FechaProxima;
                evento.EstadoEquipoResultado = model.EstadoEquipoResultado;
                evento.ComentariosAdicionales = model.ComentariosAdicionales;
                evento.EsExtraordinario = model.EsExtraordinario;
                evento.JustificacionExtraordinario = model.JustificacionExtraordinario;

                _context.EventosVerificacionResultado.RemoveRange(evento.ResultadosVerificacion);
                AgregarResultadosAlEvento(evento.EventoMetrologicoId, model.Resultados);

                await _context.SaveChangesAsync();

                var verificacionCompleta = await CargarVerificacionCompletaAsync(
                    verificacion.EventoVerificacionDatoId);

                if (verificacionCompleta == null)
                    throw new Exception("No se pudo recuperar la verificación para regenerar el PDF.");

                if (!string.IsNullOrWhiteSpace(verificacion.GoogleDriveFileId))
                    await _googleDriveService.DeleteFileAsync(verificacion.GoogleDriveFileId);

                await GenerarYSubirPdfAsync(verificacionCompleta);

                var codigoEquipo = verificacionCompleta.EventoMetrologico.Equipo.Codigo;

                var nuevasEvidencias = await SubirEvidenciasAsync(
                    evento.EventoMetrologicoId,
                    codigoEquipo,
                    model.Evidencias);

                if (nuevasEvidencias.Any())
                    _context.EvidenciasEventoMetrologico.AddRange(nuevasEvidencias);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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

            verificacion.EventoMetrologico.Evidencias =
                verificacion.EventoMetrologico.Evidencias
                    .Where(e => e.Activo)
                    .OrderByDescending(e => e.FechaCarga)
                    .ToList();

            return View(verificacion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion)
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Evidencias)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

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

        private async Task<TipoEventoMetrologico?> ObtenerTipoEventoVerificacionAsync()
        {
            return await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Verificación");
        }

        private async Task ValidarFormularioVerificacionAsync(
            VerificacionViewModel model,
            TipoEventoMetrologico? tipoEventoVerificacion)
        {
            if (!model.Resultados.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos una condición de verificación.");
            }

            if (tipoEventoVerificacion == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Verificación'.");
                return;
            }

            if (!ValidarEvidencias(model.Evidencias))
            {
                ModelState.AddModelError(string.Empty, "Solo se permiten evidencias visuales en formato imagen.");
            }

            var resultadoRegla = await _metrologyRulesService.EvaluarEventoAsync(
                model.EquipoId,
                tipoEventoVerificacion.TipoEventoMetrologicoId,
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

        private static void NormalizarResultados(VerificacionViewModel model)
        {
            model.Resultados = model.Resultados
                .Where(r => !string.IsNullOrWhiteSpace(r.DescripcionItem))
                .ToList();
        }

        private void AgregarResultadosAlEvento(
            int eventoMetrologicoId,
            List<VerificacionResultadoViewModel> resultados)
        {
            var orden = 1;

            foreach (var resultado in resultados)
            {
                _context.EventosVerificacionResultado.Add(new EventoVerificacionResultado
                {
                    EventoMetrologicoId = eventoMetrologicoId,
                    DescripcionItem = resultado.DescripcionItem,
                    Cumple = resultado.Cumple,
                    Observaciones = resultado.Observaciones,
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

        private async Task GenerarYSubirPdfAsync(EventoVerificacionDato verificacion)
        {
            var pdfBytes = _verificacionPdfService.Generar(verificacion);

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