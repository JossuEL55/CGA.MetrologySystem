using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Services.Pdf;

namespace CGA.MetrologySystem.Controllers
{
    public class MantenimientosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly MantenimientoPdfService _mantenimientoPdfService;
        private readonly IGoogleDriveService _googleDriveService;

        public MantenimientosController(
            AppDbContext context,
            MantenimientoPdfService mantenimientoPdfService,
            IGoogleDriveService googleDriveService)
        {
            _context = context;
            _mantenimientoPdfService = mantenimientoPdfService;
            _googleDriveService = googleDriveService;
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
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            return View(mantenimiento);
        }
        public async Task<IActionResult> ExportarPdf(int id)
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
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null)
            {
                return NotFound();
            }

            mantenimiento.EventoMetrologico.ActividadesMantenimiento =
                mantenimiento.EventoMetrologico.ActividadesMantenimiento
                    .OrderBy(a => a.Orden)
                    .ToList();

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
            model.Actividades = model.Actividades
                .Where(a => !string.IsNullOrWhiteSpace(a.DescripcionActividad))
                .ToList();

            if (!model.Actividades.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos una actividad de mantenimiento.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var tipoMantenimientoEvento = await _context.TiposEventoMetrologico
                .FirstOrDefaultAsync(t => t.Nombre == "Mantenimiento");

            if (tipoMantenimientoEvento == null)
            {
                ModelState.AddModelError(string.Empty, "No existe configurado el tipo de evento 'Mantenimiento'.");
                await CargarCombosAsync(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var eventoMetrologico = new EventoMetrologico
                {
                    EquipoId = model.EquipoId,
                    TipoEventoMetrologicoId = tipoMantenimientoEvento.TipoEventoMetrologicoId,
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

                var mantenimientoDato = new EventoMantenimientoDato
                {
                    EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId,
                    TipoMantenimientoId = model.TipoMantenimientoId
                };

                _context.EventosMantenimientoDato.Add(mantenimientoDato);

                var orden = 1;

                foreach (var actividad in model.Actividades)
                {
                    _context.EventosMantenimientoActividad.Add(new EventoMantenimientoActividad
                    {
                        EventoMetrologicoId = eventoMetrologico.EventoMetrologicoId,
                        DescripcionActividad = actividad.DescripcionActividad,
                        Observaciones = actividad.Observaciones,
                        Orden = orden
                    });

                    orden++;
                }

                await _context.SaveChangesAsync();

                var mantenimientoCompleto = await _context.EventosMantenimientoDato
                    .Include(m => m.TipoMantenimiento)
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.Equipo)
                            .ThenInclude(eq => eq.CaracteristicasMetrologicas)
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.ResponsableInterno)
                    .Include(m => m.EventoMetrologico)
                        .ThenInclude(e => e.ActividadesMantenimiento)
                    .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == mantenimientoDato.EventoMantenimientoDatoId);

                if (mantenimientoCompleto == null)
                {
                    throw new Exception("No se pudo recuperar el mantenimiento para generar el PDF.");
                }

                var pdfBytes = _mantenimientoPdfService.Generar(mantenimientoCompleto);

                var codigoEquipo = mantenimientoCompleto.EventoMetrologico.Equipo.Codigo;
                var fecha = mantenimientoCompleto.EventoMetrologico.FechaEvento.ToString("yyyy-MM-dd");
                var nombreArchivo = $"Mantenimiento-{codigoEquipo}-{fecha}.pdf";

                var subFolderId = await _googleDriveService.EnsureSubFolderAsync(codigoEquipo, "Mantenimientos");

                using var pdfStream = new MemoryStream(pdfBytes);

                var uploadResult = await _googleDriveService.UploadFileAsync(
                    pdfStream,
                    nombreArchivo,
                    "application/pdf",
                    subFolderId);

                mantenimientoDato.GoogleDriveFileId = uploadResult.FileId;
                mantenimientoDato.NombreArchivoPdf = uploadResult.FileName;
                mantenimientoDato.RutaPdf = uploadResult.WebViewLink;

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
                    .ToList()
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MantenimientoViewModel model)
        {
            if (id != model.EventoMantenimientoDatoId) return NotFound();

            model.Actividades = model.Actividades
                .Where(a => !string.IsNullOrWhiteSpace(a.DescripcionActividad))
                .ToList();

            if (!model.Actividades.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe mantener al menos una actividad de mantenimiento.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            try
            {
                var evento = mantenimiento.EventoMetrologico;

                evento.EquipoId = model.EquipoId;
                evento.SubtipoEventoId = model.SubtipoEventoId;
                evento.ResponsableInternoId = model.ResponsableInternoId;
                evento.FechaEvento = model.FechaEvento;
                evento.FechaProxima = model.FechaProxima;
                evento.EstadoEquipoResultado = model.EstadoEquipoResultado;
                evento.ComentariosAdicionales = model.ComentariosAdicionales;
                evento.EsExtraordinario = model.EsExtraordinario;
                evento.JustificacionExtraordinario = model.JustificacionExtraordinario;

                mantenimiento.TipoMantenimientoId = model.TipoMantenimientoId;

                _context.EventosMantenimientoActividad.RemoveRange(evento.ActividadesMantenimiento);

                var orden = 1;

                foreach (var actividad in model.Actividades)
                {
                    evento.ActividadesMantenimiento.Add(new EventoMantenimientoActividad
                    {
                        EventoMetrologicoId = evento.EventoMetrologicoId,
                        DescripcionActividad = actividad.DescripcionActividad,
                        Observaciones = actividad.Observaciones,
                        Orden = orden
                    });

                    orden++;
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el mantenimiento.");
                await CargarCombosAsync(model);
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
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            return View(mantenimiento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mantenimiento = await _context.EventosMantenimientoDato
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.ActividadesMantenimiento)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null) return NotFound();

            var evento = mantenimiento.EventoMetrologico;

            _context.EventosMantenimientoActividad.RemoveRange(evento.ActividadesMantenimiento);
            _context.EventosMantenimientoDato.Remove(mantenimiento);
            _context.EventosMetrologicos.Remove(evento);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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

            var tipoEventoMantenimiento = await _context.TiposEventoMetrologico
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Nombre == "Mantenimiento");

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
    }
}