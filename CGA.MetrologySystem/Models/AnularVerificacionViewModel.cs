using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class AnularVerificacionViewModel
    {
        public int EventoVerificacionDatoId { get; set; }
        public int EventoMetrologicoId { get; set; }
        public string CodigoEquipo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;
        public string? ResponsableInterno { get; set; }
        public string? SubtipoEvento { get; set; }
        public DateTime FechaEvento { get; set; }
        public DateTime? FechaProxima { get; set; }
        public bool EsHistorico { get; set; }

        [Required(ErrorMessage = "Debe registrar el motivo de la anulación.")]
        [StringLength(500, ErrorMessage = "El motivo de anulación no puede exceder los 500 caracteres.")]
        [Display(Name = "Motivo de anulación")]
        public string MotivoAnulacion { get; set; } = string.Empty;
    }
}
