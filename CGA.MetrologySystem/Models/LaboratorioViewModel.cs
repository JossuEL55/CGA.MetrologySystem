using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class LaboratorioViewModel
    {
        public int LaboratorioId { get; set; }

        [Required(ErrorMessage = "El nombre del laboratorio es obligatorio.")]
        [Display(Name = "Nombre")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Dirección")]
        [StringLength(300, ErrorMessage = "La dirección no puede superar los 300 caracteres.")]
        public string? Direccion { get; set; }

        [Display(Name = "Ciudad")]
        [StringLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
        public string? Ciudad { get; set; }

        [Display(Name = "País")]
        [StringLength(100, ErrorMessage = "El país no puede superar los 100 caracteres.")]
        public string? Pais { get; set; }

        [Display(Name = "Teléfono")]
        [StringLength(50, ErrorMessage = "El teléfono no puede superar los 50 caracteres.")]
        public string? Telefono { get; set; }

        [Display(Name = "Correo electrónico")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        public string? Email { get; set; }

        [Display(Name = "Sitio web")]
        [Url(ErrorMessage = "El sitio web debe tener un formato válido.")]
        [StringLength(200, ErrorMessage = "El sitio web no puede superar los 200 caracteres.")]
        public string? SitioWeb { get; set; }

        [Display(Name = "Norma de acreditación")]
        [StringLength(150, ErrorMessage = "La norma de acreditación no puede superar los 150 caracteres.")]
        public string? NormaAcreditacion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
    }
}