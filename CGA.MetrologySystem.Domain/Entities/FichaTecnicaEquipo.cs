using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class FichaTecnicaEquipo
    {
        public int FichaTecnicaEquipoId { get; set; }

        public int EquipoId { get; set; }

        public string NombreArchivoPdf { get; set; } = string.Empty;

        public string? GoogleDriveFileId { get; set; }

        public string? RutaPdf { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaUltimaGeneracion { get; set; }

        public int CantidadEventosIncluidos { get; set; }

        public bool Activa { get; set; } = true;

        public Equipo Equipo { get; set; } = null!;
    }
}