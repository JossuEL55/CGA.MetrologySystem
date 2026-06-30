namespace CGA.MetrologySystem.Domain.Entities
{
    public class EventoMantenimientoDato
    {
        public int EventoMantenimientoDatoId { get; set; }

        public int EventoMetrologicoId { get; set; }

        public int TipoMantenimientoId { get; set; }

        public EventoMetrologico EventoMetrologico { get; set; } = null!;

        public TipoMantenimiento TipoMantenimiento { get; set; } = null!;
        public string? GoogleDriveFileId { get; set; }
        public string? NombreArchivoPdf { get; set; }
        public string? RutaPdf { get; set; }
    }
}