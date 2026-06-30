namespace CGA.MetrologySystem.Models
{
    public class EvidenciaEventoViewModel
    {
        public int EvidenciaEventoMetrologicoId { get; set; }

        public string NombreArchivo { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string GoogleDriveFileId { get; set; } = string.Empty;

        public string RutaArchivo { get; set; } = string.Empty;

        public string TipoEvidencia { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public DateTime FechaCarga { get; set; }

        public bool Activo { get; set; }
    }
}