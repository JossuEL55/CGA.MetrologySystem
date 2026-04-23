using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    public class CalibracionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IGoogleDriveService _googleDriveService;

        public CalibracionesController(AppDbContext context, IGoogleDriveService googleDriveService)
        {
            _context = context;
            _googleDriveService = googleDriveService;
        }

        // INDEX
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

        // DETAILS
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

        // CREATE GET
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

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CalibracionViewModel model)
        {
            if (model.ArchivoCertificado == null || model.ArchivoCertificado.Length == 0)
            {
                ModelState.AddModelError("ArchivoCertificado", "Debe subir el certificado en PDF.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var tipoCalibracion = await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Calibración");

            if (tipoCalibracion == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Calibración'.");
                await CargarCombosAsync(model);
                return View(model);
            }

            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId);

            if (equipo == null)
            {
                ModelState.AddModelError("EquipoId", "El equipo seleccionado no existe.");
                await CargarCombosAsync(model);
                return View(model);
            }

            string? googleDriveFileId = null;
            string? nombreArchivoCertificado = null;
            string? rutaCertificado = null;

            if (model.ArchivoCertificado != null && model.ArchivoCertificado.Length > 0)
            {
                var subFolderId = await _googleDriveService.EnsureSubFolderAsync(equipo.Codigo, "Calibraciones");

                // VALIDACIÓN TEMPORAL
                if (string.IsNullOrWhiteSpace(subFolderId))
                {
                    throw new Exception("No se pudo obtener la subcarpeta 'Calibraciones' en Google Drive.");
                }

                using var stream = model.ArchivoCertificado.OpenReadStream();
                var uploadResult = await _googleDriveService.UploadFileAsync(
                    stream,
                    model.ArchivoCertificado.FileName,
                    model.ArchivoCertificado.ContentType,
                    subFolderId);

                googleDriveFileId = uploadResult.FileId;
                nombreArchivoCertificado = uploadResult.FileName;
                rutaCertificado = uploadResult.WebViewLink;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var eventoMetrologico = new EventoMetrologico
                {
                    EquipoId = model.EquipoId,
                    TipoEventoMetrologicoId = tipoCalibracion.TipoEventoMetrologicoId,
                    SubtipoEventoId = model.SubtipoEventoId,
                    ResponsableInternoId = model.ResponsableInternoId,
                    FechaEvento = model.FechaEvento,
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
                    FechaCalibracion = model.FechaCalibracion,
                    LaboratorioId = model.LaboratorioId,
                    RutaCertificado = rutaCertificado,
                    GoogleDriveFileId = googleDriveFileId,
                    NombreArchivoCertificado = nombreArchivoCertificado,
                    Observaciones = model.Observaciones
                };

                _context.EventosCalibracionDato.Add(calibracion);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la calibración.");
                await CargarCombosAsync(model);
                return View(model);
            }
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            var model = new CalibracionViewModel
            {
                EquipoId = calibracion.EventoMetrologico.EquipoId,
                SubtipoEventoId = calibracion.EventoMetrologico.SubtipoEventoId,
                ResponsableInternoId = calibracion.EventoMetrologico.ResponsableInternoId,
                FechaEvento = calibracion.EventoMetrologico.FechaEvento,
                FechaProxima = calibracion.EventoMetrologico.FechaProxima,
                EstadoEquipoResultado = calibracion.EventoMetrologico.EstadoEquipoResultado,
                ComentariosAdicionales = calibracion.EventoMetrologico.ComentariosAdicionales,
                EsExtraordinario = calibracion.EventoMetrologico.EsExtraordinario,
                JustificacionExtraordinario = calibracion.EventoMetrologico.JustificacionExtraordinario,
                NumeroCertificado = calibracion.NumeroCertificado,
                FechaCalibracion = calibracion.FechaCalibracion,
                LaboratorioId = calibracion.LaboratorioId,
                Observaciones = calibracion.Observaciones
            };

            ViewBag.EventoCalibracionDatoId = calibracion.EventoCalibracionDatoId;

            await CargarCombosAsync(model);
            return View(model);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CalibracionViewModel model)
        {
            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.EventoCalibracionDatoId = id;
                await CargarCombosAsync(model);
                return View(model);
            }

            if (model.ArchivoCertificado != null && model.ArchivoCertificado.Length > 0)
            {
                var equipo = calibracion.EventoMetrologico.Equipo;
                var subFolderId = await _googleDriveService.EnsureSubFolderAsync(equipo.Codigo, "Calibraciones");

                // VALIDACIÓN TEMPORAL
                if (string.IsNullOrWhiteSpace(subFolderId))
                {
                    throw new Exception("No se pudo obtener la subcarpeta 'Calibraciones' en Google Drive.");
                }

                using var stream = model.ArchivoCertificado.OpenReadStream();
                var uploadResult = await _googleDriveService.UploadFileAsync(
                    stream,
                    model.ArchivoCertificado.FileName,
                    model.ArchivoCertificado.ContentType,
                    subFolderId);

                calibracion.GoogleDriveFileId = uploadResult.FileId;
                calibracion.NombreArchivoCertificado = uploadResult.FileName;
                calibracion.RutaCertificado = uploadResult.WebViewLink;
            }

            try
            {
                calibracion.EventoMetrologico.EquipoId = model.EquipoId;
                calibracion.EventoMetrologico.SubtipoEventoId = model.SubtipoEventoId;
                calibracion.EventoMetrologico.ResponsableInternoId = model.ResponsableInternoId;
                calibracion.EventoMetrologico.FechaEvento = model.FechaEvento;
                calibracion.EventoMetrologico.FechaProxima = model.FechaProxima;
                calibracion.EventoMetrologico.EstadoEquipoResultado = model.EstadoEquipoResultado;
                calibracion.EventoMetrologico.ComentariosAdicionales = model.ComentariosAdicionales;
                calibracion.EventoMetrologico.EsExtraordinario = model.EsExtraordinario;
                calibracion.EventoMetrologico.JustificacionExtraordinario = model.JustificacionExtraordinario;

                calibracion.NumeroCertificado = model.NumeroCertificado;
                calibracion.FechaCalibracion = model.FechaCalibracion;
                calibracion.LaboratorioId = model.LaboratorioId;
                calibracion.Observaciones = model.Observaciones;

                _context.Update(calibracion);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar la calibración.");
                ViewBag.EventoCalibracionDatoId = id;
                await CargarCombosAsync(model);
                return View(model);
            }
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .Include(c => c.Laboratorio)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            return View(calibracion);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var calibracion = await _context.EventosCalibracionDato
                .Include(c => c.EventoMetrologico)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null) return NotFound();

            try
            {
                _context.EventosCalibracionDato.Remove(calibracion);
                _context.EventosMetrologicos.Remove(calibracion.EventoMetrologico);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al eliminar la calibración.");
                return View(calibracion);
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
    }
}