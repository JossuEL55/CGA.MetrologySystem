using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    public class VerificacionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly VerificacionPdfService _verificacionPdfService;
        private readonly IGoogleDriveService _googleDriveService;

        public VerificacionesController(
            AppDbContext context,
            VerificacionPdfService verificacionPdfService,
            IGoogleDriveService googleDriveService)
        {
            _context = context;
            _verificacionPdfService = verificacionPdfService;
            _googleDriveService = googleDriveService;
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
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

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
            model.Resultados = model.Resultados
                .Where(r => !string.IsNullOrWhiteSpace(r.DescripcionItem))
                .ToList();

            if (!model.Resultados.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos una condición de verificación.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var tipoEventoVerificacion = await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Verificación");

            if (tipoEventoVerificacion == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Verificación'.");
                await CargarCombosAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var eventoMetrologico = new EventoMetrologico
                {
                    EquipoId = model.EquipoId,
                    TipoEventoMetrologicoId = tipoEventoVerificacion.TipoEventoMetrologicoId,
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

                var verificacionDato = new EventoVerificacionDato
                {
                    EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId
                };

                _context.EventosVerificacionDato.Add(verificacionDato);
                await _context.SaveChangesAsync();

                var orden = 1;

                foreach (var resultado in model.Resultados)
                {
                    _context.EventosVerificacionResultado.Add(new EventoVerificacionResultado
                    {
                        EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId,
                        DescripcionItem = resultado.DescripcionItem,
                        Cumple = resultado.Cumple,
                        Observaciones = resultado.Observaciones,
                        Orden = orden
                    });

                    orden++;
                }

                await _context.SaveChangesAsync();

                var verificacionCompleta = await CargarVerificacionCompletaAsync(verificacionDato.EventoVerificacionDatoId);

                if (verificacionCompleta == null)
                {
                    throw new Exception("No se pudo recuperar la verificación para generar el PDF.");
                }

                var pdfBytes = _verificacionPdfService.Generar(verificacionCompleta);

                var codigoEquipo = verificacionCompleta.EventoMetrologico.Equipo.Codigo;
                var fecha = verificacionCompleta.EventoMetrologico.FechaEvento.ToString("yyyy-MM-dd");
                var nombreArchivo = $"Verificacion-{codigoEquipo}-{fecha}.pdf";

                var subFolderId = await _googleDriveService.EnsureSubFolderAsync(codigoEquipo, "Verificaciones");

                using var pdfStream = new MemoryStream(pdfBytes);

                var uploadResult = await _googleDriveService.UploadFileAsync(
                    pdfStream,
                    nombreArchivo,
                    "application/pdf",
                    subFolderId);

                verificacionDato.GoogleDriveFileId = uploadResult.FileId;
                verificacionDato.NombreArchivoPdf = uploadResult.FileName;
                verificacionDato.RutaPdf = uploadResult.WebViewLink;

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
                    .ToList()
            };

            await CargarCombosAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VerificacionViewModel model)
        {
            if (id != model.EventoVerificacionDatoId) return NotFound();

            model.Resultados = model.Resultados
                .Where(r => !string.IsNullOrWhiteSpace(r.DescripcionItem))
                .ToList();

            if (!model.Resultados.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe mantener al menos una condición de verificación.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var evento = verificacion.EventoMetrologico;

                evento.EquipoId = model.EquipoId;
                evento.SubtipoEventoId = model.SubtipoEventoId;
                evento.ResponsableInternoId = model.ResponsableInternoId;
                evento.FechaEvento = model.FechaEvento;
                evento.FechaProxima = model.FechaProxima;
                evento.EstadoEquipoResultado = model.EstadoEquipoResultado;
                evento.ComentariosAdicionales = model.ComentariosAdicionales;
                evento.EsExtraordinario = model.EsExtraordinario;
                evento.JustificacionExtraordinario = model.JustificacionExtraordinario;

                _context.EventosVerificacionResultado.RemoveRange(evento.ResultadosVerificacion);

                var orden = 1;

                foreach (var resultado in model.Resultados)
                {
                    evento.ResultadosVerificacion.Add(new EventoVerificacionResultado
                    {
                        EventoMetrologicoId = evento.EventoMetrologicoId,
                        DescripcionItem = resultado.DescripcionItem,
                        Cumple = resultado.Cumple,
                        Observaciones = resultado.Observaciones,
                        Orden = orden
                    });

                    orden++;
                }

                await _context.SaveChangesAsync();

                var verificacionCompleta = await CargarVerificacionCompletaAsync(verificacion.EventoVerificacionDatoId);

                if (verificacionCompleta == null)
                {
                    throw new Exception("No se pudo recuperar la verificación para regenerar el PDF.");
                }

                var pdfBytes = _verificacionPdfService.Generar(verificacionCompleta);

                var codigoEquipo = verificacionCompleta.EventoMetrologico.Equipo.Codigo;
                var fecha = verificacionCompleta.EventoMetrologico.FechaEvento.ToString("yyyy-MM-dd");
                var nombreArchivo = $"Verificacion-{codigoEquipo}-{fecha}.pdf";

                var subFolderId = await _googleDriveService.EnsureSubFolderAsync(codigoEquipo, "Verificaciones");

                using var pdfStream = new MemoryStream(pdfBytes);

                var uploadResult = await _googleDriveService.UploadFileAsync(
                    pdfStream,
                    nombreArchivo,
                    "application/pdf",
                    subFolderId);

                verificacion.GoogleDriveFileId = uploadResult.FileId;
                verificacion.NombreArchivoPdf = uploadResult.FileName;
                verificacion.RutaPdf = uploadResult.WebViewLink;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = verificacion.EventoVerificacionDatoId });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar la verificación.");
                await CargarCombosAsync(model);
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
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            return View(verificacion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var verificacion = await _context.EventosVerificacionDato
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.ResultadosVerificacion)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null) return NotFound();

            var evento = verificacion.EventoMetrologico;

            _context.EventosVerificacionResultado.RemoveRange(evento.ResultadosVerificacion);
            _context.EventosVerificacionDato.Remove(verificacion);
            _context.EventosMetrologicos.Remove(evento);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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

            var tipoEventoVerificacion = await _context.TiposEventoMetrologico
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Nombre == "Verificación");

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
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == eventoVerificacionDatoId);

            if (verificacion == null) return null;

            verificacion.EventoMetrologico.ResultadosVerificacion =
                verificacion.EventoMetrologico.ResultadosVerificacion
                    .OrderBy(r => r.Orden)
                    .ToList();

            return verificacion;
        }
    }
}