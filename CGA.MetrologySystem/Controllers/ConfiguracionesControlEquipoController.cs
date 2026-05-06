using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    public class ConfiguracionesControlEquipoController : Controller
    {
        private readonly AppDbContext _context;

        public ConfiguracionesControlEquipoController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var configuraciones = await _context.ConfiguracionesControlEquipo
                .Include(c => c.Equipo)
                .Include(c => c.TipoEventoMetrologico)
                .OrderBy(c => c.Equipo.Codigo)
                .ThenBy(c => c.TipoEventoMetrologico.Nombre)
                .ToListAsync();

            return View(configuraciones);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var configuracion = await _context.ConfiguracionesControlEquipo
                .Include(c => c.Equipo)
                .Include(c => c.TipoEventoMetrologico)
                .FirstOrDefaultAsync(c => c.ConfiguracionControlEquipoId == id);

            if (configuracion == null) return NotFound();

            return View(configuracion);
        }

        public async Task<IActionResult> Create()
        {
            var model = new ConfiguracionControlEquipoViewModel
            {
                RequiereControl = true,
                Activo = true,
                PeriodicidadUnidad = "Meses"
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConfiguracionControlEquipoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var existe = await _context.ConfiguracionesControlEquipo.AnyAsync(c =>
                c.EquipoId == model.EquipoId &&
                c.TipoEventoMetrologicoId == model.TipoEventoMetrologicoId);

            if (existe)
            {
                ModelState.AddModelError(string.Empty, "Ya existe una configuración para este equipo y tipo de evento.");
                await CargarCombosAsync(model);
                return View(model);
            }

            var configuracion = new ConfiguracionControlEquipo
            {
                EquipoId = model.EquipoId,
                TipoEventoMetrologicoId = model.TipoEventoMetrologicoId,
                PeriodicidadValor = model.PeriodicidadValor,
                PeriodicidadUnidad = model.PeriodicidadUnidad,
                RequiereControl = model.RequiereControl,
                PermitePorIngreso = model.PermitePorIngreso,
                Activo = model.Activo
            };

            _context.ConfiguracionesControlEquipo.Add(configuracion);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var configuracion = await _context.ConfiguracionesControlEquipo
                .FirstOrDefaultAsync(c => c.ConfiguracionControlEquipoId == id);

            if (configuracion == null) return NotFound();

            var model = new ConfiguracionControlEquipoViewModel
            {
                ConfiguracionControlEquipoId = configuracion.ConfiguracionControlEquipoId,
                EquipoId = configuracion.EquipoId,
                TipoEventoMetrologicoId = configuracion.TipoEventoMetrologicoId,
                PeriodicidadValor = configuracion.PeriodicidadValor,
                PeriodicidadUnidad = configuracion.PeriodicidadUnidad,
                RequiereControl = configuracion.RequiereControl,
                PermitePorIngreso = configuracion.PermitePorIngreso,
                Activo = configuracion.Activo
            };

            await CargarCombosAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConfiguracionControlEquipoViewModel model)
        {
            if (id != model.ConfiguracionControlEquipoId) return NotFound();

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model);
                return View(model);
            }

            var configuracion = await _context.ConfiguracionesControlEquipo
                .FirstOrDefaultAsync(c => c.ConfiguracionControlEquipoId == id);

            if (configuracion == null) return NotFound();

            var existe = await _context.ConfiguracionesControlEquipo.AnyAsync(c =>
                c.ConfiguracionControlEquipoId != id &&
                c.EquipoId == model.EquipoId &&
                c.TipoEventoMetrologicoId == model.TipoEventoMetrologicoId);

            if (existe)
            {
                ModelState.AddModelError(string.Empty, "Ya existe otra configuración para este equipo y tipo de evento.");
                await CargarCombosAsync(model);
                return View(model);
            }

            configuracion.EquipoId = model.EquipoId;
            configuracion.TipoEventoMetrologicoId = model.TipoEventoMetrologicoId;
            configuracion.PeriodicidadValor = model.PeriodicidadValor;
            configuracion.PeriodicidadUnidad = model.PeriodicidadUnidad;
            configuracion.RequiereControl = model.RequiereControl;
            configuracion.PermitePorIngreso = model.PermitePorIngreso;
            configuracion.Activo = model.Activo;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var configuracion = await _context.ConfiguracionesControlEquipo
                .Include(c => c.Equipo)
                .Include(c => c.TipoEventoMetrologico)
                .FirstOrDefaultAsync(c => c.ConfiguracionControlEquipoId == id);

            if (configuracion == null) return NotFound();

            return View(configuracion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var configuracion = await _context.ConfiguracionesControlEquipo
                .FirstOrDefaultAsync(c => c.ConfiguracionControlEquipoId == id);

            if (configuracion == null) return NotFound();

            _context.ConfiguracionesControlEquipo.Remove(configuracion);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombosAsync(ConfiguracionControlEquipoViewModel model)
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