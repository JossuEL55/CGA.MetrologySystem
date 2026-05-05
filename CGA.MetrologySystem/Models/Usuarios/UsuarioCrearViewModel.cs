using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models.Usuarios
{
    public class UsuarioCrearViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder los 100 caracteres.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$",
            ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula y un número.")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [StringLength(50)]
        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}