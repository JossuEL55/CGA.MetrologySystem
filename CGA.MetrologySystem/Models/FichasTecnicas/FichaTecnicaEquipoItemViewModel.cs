namespace CGA.MetrologySystem.Models.FichasTecnicas
{
    public class FichaTecnicaEquipoItemViewModel
    {
        public int EquipoId { get; set; }

        public int? FichaTecnicaEquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string TipoEquipo { get; set; } = string.Empty;

        public bool TieneFichaTecnica { get; set; }

        public string? NombreArchivoPdf { get; set; }

        public string? RutaPdf { get; set; }

        public DateTime? FechaUltimaGeneracion { get; set; }

        public int CantidadEventosIncluidos { get; set; }
    }
}