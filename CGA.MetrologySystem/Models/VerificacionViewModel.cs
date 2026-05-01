using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class VerificacionViewModel
    {
        public int EventoMetrologicoId { get; set; }
        public int EventoVerificacionDatoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un equipo.")]
        [Display(Name = "Equipo")]
        public int EquipoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un subtipo de evento.")]
        [Display(Name = "Subtipo de evento")]
        public int SubtipoEventoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un responsable interno.")]
        [Display(Name = "Responsable interno")]
        public int ResponsableInternoId { get; set; }

        [Required(ErrorMessage = "La fecha de verificación es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de verificación")]
        public DateTime FechaEvento { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Próxima fecha de verificación")]
        public DateTime? FechaProxima { get; set; }

        [Display(Name = "Estado del equipo")]
        public string? EstadoEquipoResultado { get; set; }

        [Display(Name = "Comentarios adicionales")]
        public string? ComentariosAdicionales { get; set; }

        [Display(Name = "Es extraordinario")]
        public bool EsExtraordinario { get; set; }

        [Display(Name = "Justificación extraordinaria")]
        public string? JustificacionExtraordinario { get; set; }

        public List<VerificacionResultadoViewModel> Resultados { get; set; } = new();

        public List<SelectListItem> Equipos { get; set; } = new();
        public List<SelectListItem> SubtiposEvento { get; set; } = new();
        public List<SelectListItem> ResponsablesInternos { get; set; } = new();
    }

    public class VerificacionResultadoViewModel
    {
        public int EventoVerificacionResultadoId { get; set; }

        [Required(ErrorMessage = "La condición a verificar es obligatoria.")]
        [Display(Name = "Condición a verificar")]
        public string DescripcionItem { get; set; } = string.Empty;

        [Display(Name = "Cumple")]
        public bool Cumple { get; set; } = true;

        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        public int Orden { get; set; }
    }
}