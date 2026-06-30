using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class EventoVerificacionDato
    {
        public int EventoVerificacionDatoId { get; set; }

        public int EventoMetrologicoId { get; set; }

        // PDF en Drive
        public string? GoogleDriveFileId { get; set; }
        public string? NombreArchivoPdf { get; set; }
        public string? RutaPdf { get; set; }

        public EventoMetrologico EventoMetrologico { get; set; } = null!;
    }
}