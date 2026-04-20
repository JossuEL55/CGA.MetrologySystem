using Microsoft.AspNetCore.Identity;

namespace CGA.MetrologySystem.Infrastructure.Identity
{
    public class UsuarioSistema : IdentityUser
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}