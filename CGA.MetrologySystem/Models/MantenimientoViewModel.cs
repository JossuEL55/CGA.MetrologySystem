using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CGA.MetrologySystem.Models
{
    public class MantenimientoViewModel
    {
        public int EventoMetrologicoId { get; set; }
        public int EventoMantenimientoDatoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un equipo.")]
        [Display(Name = "Equipo")]
        public int EquipoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un subtipo de evento.")]
        [Display(Name = "Subtipo de evento")]
        public int SubtipoEventoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de mantenimiento.")]
        [Display(Name = "Tipo de mantenimiento")]
        public int TipoMantenimientoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un responsable interno.")]
        [Display(Name = "Responsable interno")]
        public int ResponsableInternoId { get; set; }

        [Required(ErrorMessage = "La fecha del mantenimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha del mantenimiento")]
        public DateTime FechaEvento { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Próxima fecha")]
        public DateTime? FechaProxima { get; set; }

        [StringLength(50, ErrorMessage = "El estado del equipo no puede exceder los 50 caracteres.")]
        [Display(Name = "Estado del equipo")]
        public string? EstadoEquipoResultado { get; set; }

        [StringLength(500, ErrorMessage = "Los comentarios adicionales no pueden exceder los 500 caracteres.")]
        [Display(Name = "Comentarios adicionales")]
        public string? ComentariosAdicionales { get; set; }

        [Display(Name = "Registro histórico")]
        public bool EsHistorico { get; set; }

        [StringLength(500, ErrorMessage = "La observación histórica no puede exceder los 500 caracteres.")]
        [Display(Name = "Observación de carga histórica")]
        public string? ObservacionCargaHistorica { get; set; }

        [Display(Name = "Es extraordinario")]
        public bool EsExtraordinario { get; set; }

        [StringLength(500, ErrorMessage = "La justificación no puede exceder los 500 caracteres.")]
        [Display(Name = "Justificación extraordinario")]
        public string? JustificacionExtraordinario { get; set; }

        public List<MantenimientoActividadViewModel> Actividades { get; set; } = new();

        public List<SelectListItem> Equipos { get; set; } = new();
        public List<SelectListItem> SubtiposEvento { get; set; } = new();
        public List<SelectListItem> TiposMantenimiento { get; set; } = new();
        public List<SelectListItem> ResponsablesInternos { get; set; } = new();
        public List<IFormFile> Evidencias { get; set; } = new();

        public List<EvidenciaEventoViewModel> EvidenciasExistentes { get; set; } = new();
    }

    public class MantenimientoActividadViewModel
    {
        public int EventoMantenimientoActividadId { get; set; }

        [Required(ErrorMessage = "La actividad es obligatoria.")]
        [StringLength(300, ErrorMessage = "La actividad no puede exceder los 300 caracteres.")]
        [Display(Name = "Actividad realizada")]
        public string DescripcionActividad { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Display(Name = "Imagen del ítem")]
        public IFormFile? EvidenciaImagen { get; set; }

        public string? EvidenciaNombreArchivo { get; set; }
        public string? EvidenciaRutaArchivo { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo.")]
        public int Orden { get; set; }
    }
}
