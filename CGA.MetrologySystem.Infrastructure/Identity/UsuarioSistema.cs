using Microsoft.AspNetCore.Identity;

namespace CGA.MetrologySystem.Infrastructure.Identity
{
    public class UsuarioSistema : IdentityUser
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? UltimoAcceso { get; set; }
        public bool DebeCambiarContrasena { get; set; }
    }
}
