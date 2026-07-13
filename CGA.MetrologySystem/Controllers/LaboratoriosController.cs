using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{

    // GET: Laboratorios
    [Authorize(Roles = RolesSistema.SupervisionMetrologica)]
    public class LaboratoriosController : Controller
    {
        private readonly AppDbContext _context;

        public LaboratoriosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Laboratorios
        public async Task<IActionResult> Index()
        {
            var laboratorios = await _context.Set<Laboratorio>()
                .OrderBy(l => l.Nombre)
                .ToListAsync();

            return View(laboratorios);
        }

        // GET: Laboratorios/Create
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public IActionResult Create()
        {
            var model = new LaboratorioViewModel();
            return View(model);
        }

        // POST: Laboratorios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> Create(LaboratorioViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var laboratorio = new Laboratorio
            {
                Nombre = model.Nombre,
                Direccion = model.Direccion,
                Ciudad = model.Ciudad,
                Pais = model.Pais,
                Telefono = model.Telefono,
                Email = model.Email,
                SitioWeb = model.SitioWeb,
                NormaAcreditacion = model.NormaAcreditacion,
                NumeroAcreditacion = model.NumeroAcreditacion,
                Alcance = model.Alcance,
                Observaciones = model.Observaciones,
                Activo = model.Activo
            };

            _context.Set<Laboratorio>().Add(laboratorio);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Laboratorios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var laboratorio = await _context.Set<Laboratorio>()
                .FirstOrDefaultAsync(l => l.LaboratorioId == id);

            if (laboratorio == null)
                return NotFound();

            return View(laboratorio);
        }

        // GET: Laboratorios/Edit/5
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var laboratorio = await _context.Set<Laboratorio>().FindAsync(id);

            if (laboratorio == null)
                return NotFound();

            var model = new LaboratorioViewModel
            {
                LaboratorioId = laboratorio.LaboratorioId,
                Nombre = laboratorio.Nombre,
                Direccion = laboratorio.Direccion,
                Ciudad = laboratorio.Ciudad,
                Pais = laboratorio.Pais,
                Telefono = laboratorio.Telefono,
                Email = laboratorio.Email,
                SitioWeb = laboratorio.SitioWeb,
                NormaAcreditacion = laboratorio.NormaAcreditacion,
                NumeroAcreditacion = laboratorio.NumeroAcreditacion,
                Alcance = laboratorio.Alcance,
                Observaciones = laboratorio.Observaciones,
                Activo = laboratorio.Activo
            };

            return View(model);
        }

        // POST: Laboratorios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> Edit(int id, LaboratorioViewModel model)
        {
            if (id != model.LaboratorioId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var laboratorio = await _context.Set<Laboratorio>().FindAsync(id);

            if (laboratorio == null)
                return NotFound();

            laboratorio.Nombre = model.Nombre;
            laboratorio.Direccion = model.Direccion;
            laboratorio.Ciudad = model.Ciudad;
            laboratorio.Pais = model.Pais;
            laboratorio.Telefono = model.Telefono;
            laboratorio.Email = model.Email;
            laboratorio.SitioWeb = model.SitioWeb;
            laboratorio.NormaAcreditacion = model.NormaAcreditacion;
            laboratorio.NumeroAcreditacion = model.NumeroAcreditacion;
            laboratorio.Alcance = model.Alcance;
            laboratorio.Observaciones = model.Observaciones;
            laboratorio.Activo = model.Activo;

            _context.Set<Laboratorio>().Update(laboratorio);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Laboratorios/Delete/5
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var laboratorio = await _context.Set<Laboratorio>()
                .FirstOrDefaultAsync(l => l.LaboratorioId == id);

            if (laboratorio == null)
                return NotFound();

            return View(laboratorio);
        }

        // POST: Laboratorios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.GestionMetrologica)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var laboratorio = await _context.Set<Laboratorio>().FindAsync(id);

            if (laboratorio == null)
                return NotFound();

            _context.Set<Laboratorio>().Remove(laboratorio);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
