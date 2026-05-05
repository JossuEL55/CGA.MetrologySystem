using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models.Perfil
{
    public class PerfilViewModel
    {
        [Display(Name = "Correo")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$",
            ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        public string Rol { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }
}