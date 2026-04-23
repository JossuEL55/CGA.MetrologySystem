using System.ComponentModel.DataAnnotations.Schema;

namespace CGA.MetrologySystem.Domain.Entities

{
    public class EventoCalibracionDato
    {
        public int EventoCalibracionDatoId { get; set; }

        public int EventoMetrologicoId { get; set; }

        public string? NumeroCertificado { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaCalibracion { get; set; }

        public int? LaboratorioId { get; set; }
        public Laboratorio? Laboratorio { get; set; }

        public string? RutaCertificado { get; set; }

        // Datos del archivo subido a Google Drive
        public string? GoogleDriveFileId { get; set; }
        public string? NombreArchivoCertificado { get; set; }

        public string? Observaciones { get; set; }

        public EventoMetrologico EventoMetrologico { get; set; } = null!;
    }
}