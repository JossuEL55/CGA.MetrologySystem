using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class ConfiguracionControlEquipoViewModel
    {
        public int ConfiguracionControlEquipoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un equipo.")]
        [Display(Name = "Equipo")]
        public int EquipoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de evento.")]
        [Display(Name = "Tipo de evento")]
        public int TipoEventoMetrologicoId { get; set; }

        [Required(ErrorMessage = "Debe ingresar la periodicidad.")]
        [Range(1, 120, ErrorMessage = "La periodicidad debe estar entre 1 y 120.")]
        [Display(Name = "Periodicidad")]
        public int? PeriodicidadValor { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la unidad de periodicidad.")]
        [Display(Name = "Unidad")]
        public string? PeriodicidadUnidad { get; set; }

        [Display(Name = "Requiere control")]
        public bool RequiereControl { get; set; } = true;

        [Display(Name = "Permite por ingreso")]
        public bool PermitePorIngreso { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public List<SelectListItem> Equipos { get; set; } = new();
        public List<SelectListItem> TiposEventoMetrologico { get; set; } = new();

        public List<SelectListItem> UnidadesPeriodicidad { get; set; } = new()
        {
            new SelectListItem { Value = "Dias", Text = "Días" },
            new SelectListItem { Value = "Meses", Text = "Meses" },
            new SelectListItem { Value = "Anios", Text = "Años" }
        };
    }
}