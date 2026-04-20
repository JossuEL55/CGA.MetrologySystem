using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models.Perfil
{
    //Modelo para la vista de cambio de contraseña de usuario, con validaciones de datos
    public class CambiarContrasenaViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarNuevaContrasena { get; set; } = string.Empty;
    }
}