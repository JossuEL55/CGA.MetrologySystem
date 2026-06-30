namespace CGA.MetrologySystem.Domain.Entities
{
    public class EvidenciaEventoMetrologico
    {
        public int EvidenciaEventoMetrologicoId { get; set; }

        public int EventoMetrologicoId { get; set; }
        public EventoMetrologico EventoMetrologico { get; set; } = null!;

        public string NombreArchivo { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string GoogleDriveFileId { get; set; } = string.Empty;

        public string RutaArchivo { get; set; } = string.Empty;

        public string TipoEvidencia { get; set; } = "Imagen";

        public string? Descripcion { get; set; }

        public DateTime FechaCarga { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;
    }
}