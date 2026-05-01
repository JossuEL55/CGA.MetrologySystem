using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class PlantillaEventoViewModel
    {
        public int PlantillaEventoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de equipo.")]
        [Display(Name = "Tipo de equipo")]
        public int TipoEquipoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de evento.")]
        [Display(Name = "Tipo de evento")]
        public int TipoEventoMetrologicoId { get; set; }

        [Required(ErrorMessage = "El nombre de la plantilla es obligatorio.")]
        [Display(Name = "Nombre de la plantilla")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public List<PlantillaEventoItemViewModel> Items { get; set; } = new();

        public List<SelectListItem> TiposEquipo { get; set; } = new();

        public List<SelectListItem> TiposEventoMetrologico { get; set; } = new();
    }

    public class PlantillaEventoItemViewModel
    {
        public int PlantillaEventoItemId { get; set; }

        [Required(ErrorMessage = "La descripción del ítem es obligatoria.")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        public int Orden { get; set; }

        public bool Activo { get; set; } = true;
    }
}