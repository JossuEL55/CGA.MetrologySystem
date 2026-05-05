using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class LaboratorioViewModel
    {
        public int LaboratorioId { get; set; }

        [Required(ErrorMessage = "El nombre del laboratorio es obligatorio.")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.\-]+$",
            ErrorMessage = "El nombre contiene caracteres no válidos.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La dirección no puede superar los 300 caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s\-]+$",
            ErrorMessage = "La ciudad contiene caracteres no válidos.")]
        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }

        [StringLength(100, ErrorMessage = "El país no puede superar los 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s\-]+$",
            ErrorMessage = "El país contiene caracteres no válidos.")]
        [Display(Name = "País")]
        public string? Pais { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
        [RegularExpression(@"^\+?[0-9\s\-]{7,15}$",
        ErrorMessage = "El teléfono debe contener solo números, espacios, guiones y puede iniciar con +.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string? Email { get; set; }

        [Url(ErrorMessage = "El sitio web debe tener un formato válido.")]
        [StringLength(200, ErrorMessage = "El sitio web no puede superar los 200 caracteres.")]
        [Display(Name = "Sitio web")]
        public string? SitioWeb { get; set; }

        [StringLength(150, ErrorMessage = "La norma de acreditación no puede superar los 150 caracteres.")]
        [Display(Name = "Norma de acreditación")]
        public string? NormaAcreditacion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
    }
}