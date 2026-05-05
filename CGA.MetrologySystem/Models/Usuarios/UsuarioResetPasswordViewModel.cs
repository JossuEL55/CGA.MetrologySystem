using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models.Usuarios
{
    public class UsuarioResetPasswordViewModel
    {
        [Required(ErrorMessage = "El identificador del usuario es obligatorio.")]
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula y un número.")]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}