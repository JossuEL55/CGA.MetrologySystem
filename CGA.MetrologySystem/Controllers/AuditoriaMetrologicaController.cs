using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.SupervisionMetrologica)]
    public class AuditoriaMetrologicaController : Controller
    {
        private readonly AppDbContext _context;

        public AuditoriaMetrologicaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var fechaUltimas24Horas = DateTime.UtcNow.AddHours(-24);

            var resumen = await _context.AuditoriasMetrologicas
                .AsNoTracking()
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Criticos = g.Count(a => a.EsCritico),
                    Ultimas24Horas = g.Count(a => a.Fecha >= fechaUltimas24Horas)
                })
                .FirstOrDefaultAsync();

            var registros = await _context.AuditoriasMetrologicas
                .AsNoTracking()
                .OrderByDescending(a => a.Fecha)
                .Take(150)
                .Select(a => new AuditoriaMetrologicaListadoViewModel
                {
                    Fecha = a.Fecha,
                    Usuario = string.IsNullOrWhiteSpace(a.UsuarioCorreo)
                        ? a.UsuarioNombre
                        : $"{a.UsuarioNombre} ({a.UsuarioCorreo})",
                    RolUsuario = a.RolUsuario,
                    Accion = a.Accion,
                    Entidad = a.Entidad,
                    CodigoEquipo = a.CodigoEquipo,
                    NombreEquipo = a.NombreEquipo,
                    TipoEvento = a.TipoEvento,
                    Detalle = a.Detalle,
                    EsCritico = a.EsCritico
                })
                .ToListAsync();

            return View(new AuditoriaMetrologicaIndexViewModel
            {
                TotalRegistros = resumen?.Total ?? 0,
                CambiosCriticos = resumen?.Criticos ?? 0,
                RegistrosUltimas24Horas = resumen?.Ultimas24Horas ?? 0,
                Registros = registros
            });
        }
    }
}
