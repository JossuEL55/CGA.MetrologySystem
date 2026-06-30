using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models.Usuarios
{
    public class UsuarioListadoViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        public string Correo { get; set; } = string.Empty;

        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;
        public bool Activo { get; set; }

        [Display(Name = "Rol")]
        public string Rol { get; set; } = string.Empty;
    }
}