using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace CGA.MetrologySystem.Controllers
{
    public class EquiposController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IGoogleDriveService _googleDriveService;

        public EquiposController(AppDbContext context, IGoogleDriveService googleDriveService)
        {
            _context = context;
            _googleDriveService = googleDriveService;
        }

        // GET: Equipos
        public async Task<IActionResult> Index()
        {
            var equipos = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Proveedor)
                .Include(e => e.Ubicacion)
                .Include(e => e.ResponsableInterno)
                .ToListAsync();

            return View(equipos);
        }

        // GET: Equipos/Create
        public async Task<IActionResult> Create()
        {
            var model = new EquipoViewModel();
            await CargarCombos(model);
            return View(model);
        }

        // POST: Equipos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EquipoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarCombos(model);
                return View(model);
            }

            var equipo = new Equipo
            {
                Codigo = model.Codigo,
                Nombre = model.Nombre,
                TipoEquipoId = model.TipoEquipoId,
                ProveedorId = model.ProveedorId,
                UbicacionId = model.UbicacionId,
                ResponsableInternoId = model.ResponsableInternoId,
                Marca = model.Marca,
                Modelo = model.Modelo,
                Serie = model.Serie,
                Identificacion = model.Identificacion,
                FechaAdquisicion = model.FechaAdquisicion,
                FechaPuestaFuncionamiento = model.FechaPuestaFuncionamiento,
                FabricanteLugarOrigen = model.FabricanteLugarOrigen,
                CatalogoManejoOperacion = model.CatalogoManejoOperacion,
                MantenimientoFabricante = model.MantenimientoFabricante,
                CondicionesOperacion = model.CondicionesOperacion,
                Activo = model.Activo
            };

            _context.Equipos.Add(equipo);
            await _context.SaveChangesAsync();

            var folderId = await _googleDriveService.EnsureEquipoFolderAsync(equipo.Codigo);
            equipo.GoogleDriveFolderId = folderId;

            _context.Equipos.Update(equipo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Equipos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Proveedor)
                .Include(e => e.Ubicacion)
                .Include(e => e.ResponsableInterno)
                .FirstOrDefaultAsync(e => e.EquipoId == id);

            if (equipo == null)
                return NotFound();

            return View(equipo);
        }

        // GET: Equipos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var equipo = await _context.Equipos.FindAsync(id);

            if (equipo == null)
                return NotFound();

            var model = new EquipoViewModel
            {
                EquipoId = equipo.EquipoId,
                Codigo = equipo.Codigo,
                Nombre = equipo.Nombre,
                TipoEquipoId = equipo.TipoEquipoId,
                ProveedorId = equipo.ProveedorId,
                UbicacionId = equipo.UbicacionId,
                ResponsableInternoId = equipo.ResponsableInternoId,
                Marca = equipo.Marca,
                Modelo = equipo.Modelo,
                Serie = equipo.Serie,
                Identificacion = equipo.Identificacion,
                FechaAdquisicion = equipo.FechaAdquisicion,
                FechaPuestaFuncionamiento = equipo.FechaPuestaFuncionamiento,
                FabricanteLugarOrigen = equipo.FabricanteLugarOrigen,
                CatalogoManejoOperacion = equipo.CatalogoManejoOperacion,
                MantenimientoFabricante = equipo.MantenimientoFabricante,
                CondicionesOperacion = equipo.CondicionesOperacion,
                Activo = equipo.Activo
            };

            await CargarCombos(model);
            return View(model);
        }

        // POST: Equipos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EquipoViewModel model)
        {
            if (id != model.EquipoId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await CargarCombos(model);
                return View(model);
            }

            var equipo = await _context.Equipos.FindAsync(id);

            if (equipo == null)
                return NotFound();

            equipo.Codigo = model.Codigo;
            equipo.Nombre = model.Nombre;
            equipo.TipoEquipoId = model.TipoEquipoId;
            equipo.ProveedorId = model.ProveedorId;
            equipo.UbicacionId = model.UbicacionId;
            equipo.ResponsableInternoId = model.ResponsableInternoId;
            equipo.Marca = model.Marca;
            equipo.Modelo = model.Modelo;
            equipo.Serie = model.Serie;
            equipo.Identificacion = model.Identificacion;
            equipo.FechaAdquisicion = model.FechaAdquisicion;
            equipo.FechaPuestaFuncionamiento = model.FechaPuestaFuncionamiento;
            equipo.FabricanteLugarOrigen = model.FabricanteLugarOrigen;
            equipo.CatalogoManejoOperacion = model.CatalogoManejoOperacion;
            equipo.MantenimientoFabricante = model.MantenimientoFabricante;
            equipo.CondicionesOperacion = model.CondicionesOperacion;
            equipo.Activo = model.Activo;

            _context.Update(equipo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Equipos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Proveedor)
                .Include(e => e.Ubicacion)
                .Include(e => e.ResponsableInterno)
                .FirstOrDefaultAsync(e => e.EquipoId == id);

            if (equipo == null)
                return NotFound();

            return View(equipo);
        }

        // POST: Equipos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);

            if (equipo == null)
                return NotFound();

            _context.Equipos.Remove(equipo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombos(EquipoViewModel model)
        {
            model.TiposEquipo = await _context.TiposEquipo
                .Where(t => true) // luego puedes filtrar por activos si agregas ese campo
                .Select(t => new SelectListItem
                {
                    Value = t.TipoEquipoId.ToString(),
                    Text = t.Nombre
                })
                .ToListAsync();

            model.Proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .Select(p => new SelectListItem
                {
                    Value = p.ProveedorId.ToString(),
                    Text = p.Nombre
                })
                .ToListAsync();

            model.Ubicaciones = await _context.Ubicaciones
                .Where(u => u.Activo)
                .Select(u => new SelectListItem
                {
                    Value = u.UbicacionId.ToString(),
                    Text = u.Nombre
                })
                .ToListAsync();

            model.ResponsablesInternos = await _context.ResponsablesInternos
                .Where(r => r.Activo)
                .Select(r => new SelectListItem
                {
                    Value = r.ResponsableInternoId.ToString(),
                    Text = r.NombreCompleto
                })
                .ToListAsync();
        }
    }
}