using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    //controlador para gestionar la auditoría de acciones realizadas por los administradores en el sistema,
    //permitiendo visualizar un historial de actividades con detalles como fecha, acción realizada, }
    //usuario afectado y correo del administrador responsable
    [Authorize(Roles = "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly AppDbContext _context;

        public AuditoriaController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var modelo = await _context.AuditoriasUsuario
                .OrderByDescending(a => a.Fecha)
                .Select(a => new AuditoriaUsuarioListadoViewModel
                {
                    Fecha = a.Fecha,
                    AdministradorCorreo = a.AdministradorCorreo,
                    Accion = a.Accion,
                    UsuarioAfectadoCorreo = a.UsuarioAfectadoCorreo,
                    Detalle = a.Detalle
                })
                .ToListAsync();

            return View(modelo);
        }
    }
}