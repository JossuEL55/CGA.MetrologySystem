using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize]
    public class CalibracionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IMetrologyRulesService _metrologyRulesService;

        public CalibracionesController(
            AppDbContext context,
            IGoogleDriveService googleDriveService,
            IMetrologyRulesService metrologyRulesService)
        {
            _context = context;
            _googleDriveService = googleDriveService;
            _metrologyRulesService = metrologyRulesService;
        }

        public async Task<IActionResult> Index()
        {
            var calibraciones = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(c => c.Laboratorio)
                .OrderByDescending(c => c.EventoMetrologico.FechaEvento)
                .ToListAsync();

            return View(calibraciones);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(c => c.Laboratorio)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            return View(calibracion);
        }

        public async Task<IActionResult> Create()
        {
            var model = new CalibracionViewModel
            {
                FechaEvento = DateTime.Today,
                FechaCalibracion = DateTime.Today
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CalibracionViewModel model)
        {
            var tipoCalibracion = await ObtenerTipoEventoCalibracionAsync();

            await ValidarFormularioCalibracionAsync(
                model,
                tipoCalibracion,
                certificadoObligatorio: true);

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var equipo = await _context.Equipos
                    .FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId);

                if (equipo == null)
                {
                    ModelState.AddModelError("EquipoId", "El equipo seleccionado no existe.");
                    await CargarCombosAsync(model);
                    return View(model);
                }

                var uploadResult = await SubirCertificadoAsync(
                    equipo.Codigo,
                    model.ArchivoCertificado!);

                var eventoMetrologico = new EventoMetrologico
                {
                    EquipoId = model.EquipoId,
                    TipoEventoMetrologicoId = tipoCalibracion!.TipoEventoMetrologicoId,
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

                var calibracion = new EventoCalibracionDato
                {
                    EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId,
                    NumeroCertificado = model.NumeroCertificado,
                    FechaCalibracion = model.FechaCalibracion!.Value.Date,
                    LaboratorioId = model.LaboratorioId,
                    RutaCertificado = uploadResult.WebViewLink,
                    GoogleDriveFileId = uploadResult.FileId,
                    NombreArchivoCertificado = uploadResult.FileName,
                    Observaciones = model.Observaciones
                };

                _context.EventosCalibracionDato.Add(calibracion);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = calibracion.EventoCalibracionDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la calibración.");
                await CargarCombosAsync(model);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            var evento = calibracion.EventoMetrologico;

            var model = new CalibracionViewModel
            {
                EquipoId = evento.EquipoId,
                SubtipoEventoId = evento.SubtipoEventoId,
                ResponsableInternoId = evento.ResponsableInternoId,
                FechaEvento = evento.FechaEvento,
                FechaProxima = evento.FechaProxima,
                EstadoEquipoResultado = evento.EstadoEquipoResultado,
                ComentariosAdicionales = evento.ComentariosAdicionales,
                EsExtraordinario = evento.EsExtraordinario,
                JustificacionExtraordinario = evento.JustificacionExtraordinario,
                NumeroCertificado = calibracion.NumeroCertificado,
                FechaCalibracion = calibracion.FechaCalibracion,
                LaboratorioId = calibracion.LaboratorioId,
                Observaciones = calibracion.Observaciones
            };

            ViewBag.EventoCalibracionDatoId = calibracion.EventoCalibracionDatoId;
            ViewBag.NombreArchivoCertificado = calibracion.NombreArchivoCertificado;
            ViewBag.RutaCertificado = calibracion.RutaCertificado;

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CalibracionViewModel model)
        {
            var tipoCalibracion = await ObtenerTipoEventoCalibracionAsync();

            await ValidarFormularioCalibracionAsync(
                model,
                tipoCalibracion,
                certificadoObligatorio: false);

            if (!ModelState.IsValid)
            {
                ViewBag.EventoCalibracionDatoId = id;
                await CargarCombosAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var calibracion = await _context.EventosCalibracionDato
                    .Include(c => c.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                    .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

                if (calibracion == null) return NotFound();

                var evento = calibracion.EventoMetrologico;

                evento.EquipoId = model.EquipoId;
                evento.TipoEventoMetrologicoId = tipoCalibracion!.TipoEventoMetrologicoId;
                evento.SubtipoEventoId = model.SubtipoEventoId;
                evento.ResponsableInternoId = model.ResponsableInternoId;
                evento.FechaEvento = model.FechaEvento.Date;
                evento.FechaProxima = model.FechaProxima;
                evento.EstadoEquipoResultado = model.EstadoEquipoResultado;
                evento.ComentariosAdicionales = model.ComentariosAdicionales;
                evento.EsExtraordinario = model.EsExtraordinario;
                evento.JustificacionExtraordinario = model.JustificacionExtraordinario;

                calibracion.NumeroCertificado = model.NumeroCertificado;
                calibracion.FechaCalibracion = model.FechaCalibracion!.Value.Date; calibracion.LaboratorioId = model.LaboratorioId;
                calibracion.Observaciones = model.Observaciones;

                if (model.ArchivoCertificado != null && model.ArchivoCertificado.Length > 0)
                {
                    var equipo = await _context.Equipos
                        .FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId);

                    if (equipo == null)
                    {
                        ModelState.AddModelError("EquipoId", "El equipo seleccionado no existe.");
                        ViewBag.EventoCalibracionDatoId = id;
                        await CargarCombosAsync(model);
                        return View(model);
                    }

                    if (!string.IsNullOrWhiteSpace(calibracion.GoogleDriveFileId))
                    {
                        await _googleDriveService.DeleteFileAsync(calibracion.GoogleDriveFileId);
                    }

                    var uploadResult = await SubirCertificadoAsync(
                        equipo.Codigo,
                        model.ArchivoCertificado);

                    calibracion.GoogleDriveFileId = uploadResult.FileId;
                    calibracion.NombreArchivoCertificado = uploadResult.FileName;
                    calibracion.RutaCertificado = uploadResult.WebViewLink;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = calibracion.EventoCalibracionDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar la calibración.");
                ViewBag.EventoCalibracionDatoId = id;
                await CargarCombosAsync(model);
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.ResponsableInterno)
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.SubtipoEvento)
                .Include(c => c.Laboratorio)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            return View(calibracion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!string.IsNullOrWhiteSpace(calibracion.GoogleDriveFileId))
                {
                    await _googleDriveService.DeleteFileAsync(calibracion.GoogleDriveFileId);
                }

                _context.EventosCalibracionDato.Remove(calibracion);
                _context.EventosMetrologicos.Remove(calibracion.EventoMetrologico);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] = "Ocurrió un error al eliminar la calibración.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task CargarCombosAsync(CalibracionViewModel model)
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

            model.Laboratorios = await _context.Laboratorios
                .Where(l => l.Activo)
                .OrderBy(l => l.Nombre)
                .Select(l => new SelectListItem
                {
                    Value = l.LaboratorioId.ToString(),
                    Text = l.Nombre
                })
                .ToListAsync();
        }

        private async Task<TipoEventoMetrologico?> ObtenerTipoEventoCalibracionAsync()
        {
            return await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Calibración");
        }

        private async Task ValidarFormularioCalibracionAsync(
            CalibracionViewModel model,
            TipoEventoMetrologico? tipoCalibracion,
            bool certificadoObligatorio)
        {
            if (tipoCalibracion == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Calibración'.");
                return;
            }

            var equipoExiste = await _context.Equipos
                .AnyAsync(e => e.EquipoId == model.EquipoId && e.Activo);

            if (!equipoExiste)
            {
                ModelState.AddModelError("EquipoId", "Debe seleccionar un equipo válido.");
            }

            if (certificadoObligatorio &&
                (model.ArchivoCertificado == null || model.ArchivoCertificado.Length == 0))
            {
                ModelState.AddModelError("ArchivoCertificado", "Debe subir el certificado en PDF.");
            }

            if (model.ArchivoCertificado != null && model.ArchivoCertificado.Length > 0)
            {
                if (!EsPdf(model.ArchivoCertificado))
                {
                    ModelState.AddModelError("ArchivoCertificado", "El certificado debe ser un archivo PDF.");
                }
            }

            var resultadoRegla = await _metrologyRulesService.EvaluarEventoAsync(
                model.EquipoId,
                tipoCalibracion.TipoEventoMetrologicoId,
                model.FechaEvento.Date,
                model.JustificacionExtraordinario);

            model.EsExtraordinario = resultadoRegla.EsExtraordinario;
            model.FechaProxima = resultadoRegla.FechaProximaCalculada;

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

        private async Task<CGA.MetrologySystem.Application.DTOs.GoogleDriveUploadResultDto> SubirCertificadoAsync(
            string codigoEquipo,
            IFormFile archivoCertificado)
        {
            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                codigoEquipo,
                "Documentos",
                "Calibraciones");

            if (string.IsNullOrWhiteSpace(folderId))
            {
                throw new Exception("No se pudo obtener la carpeta de calibraciones en Google Drive.");
            }

            await using var stream = archivoCertificado.OpenReadStream();

            var extension = Path.GetExtension(archivoCertificado.FileName);
            var nombreLimpio = Path.GetFileNameWithoutExtension(archivoCertificado.FileName);
            var nombreArchivoDrive =
                $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{nombreLimpio}{extension}";

            return await _googleDriveService.UploadFileAsync(
                stream,
                nombreArchivoDrive,
                archivoCertificado.ContentType,
                folderId);
        }

        private static bool EsPdf(IFormFile archivo)
        {
            var extension = Path.GetExtension(archivo.FileName);

            return archivo.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }
    }
}