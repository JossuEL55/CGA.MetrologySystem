using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models.Perfil
{
    //Modelo para la vista de perfil de usuario, con validaciones de datos
    public class PerfilViewModel
    {
        [Display(Name = "Correo")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        public string Rol { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }
}