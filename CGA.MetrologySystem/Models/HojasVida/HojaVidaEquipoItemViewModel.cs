namespace CGA.MetrologySystem.Models.HojasVida
{
    public class HojaVidaEquipoItemViewModel
    {
        public int EquipoId { get; set; }

        public int? HojaVidaEquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string TipoEquipo { get; set; } = string.Empty;

        public bool TieneHojaVida { get; set; }

        public string? NombreArchivoPdf { get; set; }

        public string? RutaPdf { get; set; }

        public DateTime? FechaUltimaGeneracion { get; set; }

        public int CantidadEventosIncluidos { get; set; }
    }
}