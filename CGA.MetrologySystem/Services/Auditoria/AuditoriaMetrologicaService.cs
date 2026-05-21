using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;

namespace CGA.MetrologySystem.Services.Auditoria
{
    public class AuditoriaMetrologicaService : IAuditoriaMetrologicaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditoriaMetrologicaService> _logger;

        public AuditoriaMetrologicaService(
            AppDbContext context,
            ILogger<AuditoriaMetrologicaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RegistrarAsync(AuditoriaMetrologicaRegistro registro)
        {
            AuditoriaMetrologica? auditoria = null;

            try
            {
                auditoria = new AuditoriaMetrologica
                {
                    Fecha = DateTime.UtcNow,
                    UsuarioId = Limitar(registro.UsuarioId, 450),
                    UsuarioNombre = Limitar(registro.UsuarioNombre, 180) ?? "Usuario no identificado",
                    UsuarioCorreo = Limitar(registro.UsuarioCorreo, 256) ?? string.Empty,
                    RolUsuario = Limitar(registro.RolUsuario, 80) ?? string.Empty,
                    Accion = Limitar(registro.Accion, 120) ?? string.Empty,
                    Entidad = Limitar(registro.Entidad, 120) ?? string.Empty,
                    EntidadId = Limitar(registro.EntidadId, 80) ?? string.Empty,
                    EquipoId = registro.EquipoId,
                    CodigoEquipo = Limitar(registro.CodigoEquipo, 80) ?? string.Empty,
                    NombreEquipo = Limitar(registro.NombreEquipo, 220) ?? string.Empty,
                    EventoMetrologicoId = registro.EventoMetrologicoId,
                    TipoEvento = Limitar(registro.TipoEvento, 80) ?? string.Empty,
                    Detalle = Limitar(registro.Detalle, 1000) ?? string.Empty,
                    EsCritico = registro.EsCritico
                };

                _context.AuditoriasMetrologicas.Add(auditoria);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (auditoria != null)
                {
                    _context.Entry(auditoria).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

                _logger.LogError(
                    ex,
                    "No se pudo registrar la auditoria metrologica {Accion} para {Entidad} {EntidadId}.",
                    registro.Accion,
                    registro.Entidad,
                    registro.EntidadId);
            }
        }

        private static string? Limitar(string? valor, int longitudMaxima)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var valorNormalizado = valor.Trim();
            return valorNormalizado.Length <= longitudMaxima
                ? valorNormalizado
                : valorNormalizado[..longitudMaxima];
        }
    }
}
