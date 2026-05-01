using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    public class PlantillasEventoController : Controller
    {
        private readonly AppDbContext _context;

        public PlantillasEventoController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var plantillas = await _context.PlantillasEvento
                .Include(p => p.TipoEquipo)
                .Include(p => p.TipoEventoMetrologico)
                .Include(p => p.Items)
                .OrderBy(p => p.TipoEquipo.Nombre)
                .ThenBy(p => p.TipoEventoMetrologico.Nombre)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return View(plantillas);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var plantilla = await _context.PlantillasEvento
                .Include(p => p.TipoEquipo)
                .Include(p => p.TipoEventoMetrologico)
                .Include(p => p.Items.OrderBy(i => i.Orden))
                .FirstOrDefaultAsync(p => p.PlantillaEventoId == id);

            if (plantilla == null) return NotFound();

            return View(plantilla);
        }

        public async Task<IActionResult> Create()
        {
            var model = new PlantillaEventoViewModel
            {
                Activo = true,
                Items =
                {
                    new PlantillaEventoItemViewModel { Orden = 1, Activo = true }
                }
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlantillaEventoViewModel model)
        {
            model.Items = model.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Descripcion))
                .ToList();

            if (!model.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe agregar al menos un ítem a la plantilla.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var existePlantilla = await _context.PlantillasEvento.AnyAsync(p =>
                p.TipoEquipoId == model.TipoEquipoId &&
                p.TipoEventoMetrologicoId == model.TipoEventoMetrologicoId &&
                p.Nombre == model.Nombre);

            if (existePlantilla)
            {
                ModelState.AddModelError("Nombre", "Ya existe una plantilla con este nombre para el tipo de equipo y tipo de evento seleccionados.");
                await CargarCombosAsync(model);
                return View(model);
            }

            var plantilla = new PlantillaEvento
            {
                TipoEquipoId = model.TipoEquipoId,
                TipoEventoMetrologicoId = model.TipoEventoMetrologicoId,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Activo = model.Activo
            };

            var orden = 1;

            foreach (var item in model.Items)
            {
                plantilla.Items.Add(new PlantillaEventoItem
                {
                    Descripcion = item.Descripcion,
                    Orden = orden,
                    Activo = item.Activo
                });

                orden++;
            }

            _context.PlantillasEvento.Add(plantilla);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var plantilla = await _context.PlantillasEvento
                .Include(p => p.Items.OrderBy(i => i.Orden))
                .FirstOrDefaultAsync(p => p.PlantillaEventoId == id);

            if (plantilla == null) return NotFound();

            var model = new PlantillaEventoViewModel
            {
                PlantillaEventoId = plantilla.PlantillaEventoId,
                TipoEquipoId = plantilla.TipoEquipoId,
                TipoEventoMetrologicoId = plantilla.TipoEventoMetrologicoId,
                Nombre = plantilla.Nombre,
                Descripcion = plantilla.Descripcion,
                Activo = plantilla.Activo,
                Items = plantilla.Items
                    .OrderBy(i => i.Orden)
                    .Select(i => new PlantillaEventoItemViewModel
                    {
                        PlantillaEventoItemId = i.PlantillaEventoItemId,
                        Descripcion = i.Descripcion,
                        Orden = i.Orden,
                        Activo = i.Activo
                    })
                    .ToList()
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlantillaEventoViewModel model)
        {
            if (id != model.PlantillaEventoId) return NotFound();

            model.Items = model.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Descripcion))
                .ToList();

            if (!model.Items.Any())
            {
                ModelState.AddModelError(string.Empty, "Debe mantener al menos un ítem en la plantilla.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var plantilla = await _context.PlantillasEvento
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PlantillaEventoId == id);

            if (plantilla == null) return NotFound();

            var existePlantilla = await _context.PlantillasEvento.AnyAsync(p =>
                p.PlantillaEventoId != id &&
                p.TipoEquipoId == model.TipoEquipoId &&
                p.TipoEventoMetrologicoId == model.TipoEventoMetrologicoId &&
                p.Nombre == model.Nombre);

            if (existePlantilla)
            {
                ModelState.AddModelError("Nombre", "Ya existe otra plantilla con este nombre para el tipo de equipo y tipo de evento seleccionados.");
                await CargarCombosAsync(model);
                return View(model);
            }

            plantilla.TipoEquipoId = model.TipoEquipoId;
            plantilla.TipoEventoMetrologicoId = model.TipoEventoMetrologicoId;
            plantilla.Nombre = model.Nombre;
            plantilla.Descripcion = model.Descripcion;
            plantilla.Activo = model.Activo;

            _context.PlantillasEventoItem.RemoveRange(plantilla.Items);

            var orden = 1;

            foreach (var item in model.Items)
            {
                plantilla.Items.Add(new PlantillaEventoItem
                {
                    PlantillaEventoId = plantilla.PlantillaEventoId,
                    Descripcion = item.Descripcion,
                    Orden = orden,
                    Activo = item.Activo
                });

                orden++;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var plantilla = await _context.PlantillasEvento
                .Include(p => p.TipoEquipo)
                .Include(p => p.TipoEventoMetrologico)
                .Include(p => p.Items.OrderBy(i => i.Orden))
                .FirstOrDefaultAsync(p => p.PlantillaEventoId == id);

            if (plantilla == null) return NotFound();

            return View(plantilla);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var plantilla = await _context.PlantillasEvento
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PlantillaEventoId == id);

            if (plantilla == null) return NotFound();

            _context.PlantillasEventoItem.RemoveRange(plantilla.Items);
            _context.PlantillasEvento.Remove(plantilla);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombosAsync(PlantillaEventoViewModel model)
        {
            model.TiposEquipo = await _context.TiposEquipo
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.TipoEquipoId.ToString(),
                    Text = t.Nombre
                })
                .ToListAsync();

            model.TiposEventoMetrologico = await _context.TiposEventoMetrologico
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.TipoEventoMetrologicoId.ToString(),
                    Text = t.Nombre
                })
                .ToListAsync();
        }
    }
}