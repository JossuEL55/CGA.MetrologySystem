namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlEventoOperativoViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public int? TipoEquipoId { get; set; }

        public string TipoEquipo { get; set; } = string.Empty;

        public int TipoEventoMetrologicoId { get; set; }

        public string TipoEventoNombre { get; set; } = string.Empty;

        public EstadoControlMetrologico Estado { get; set; }

        public DateTime? FechaUltimoEvento { get; set; }

        public DateTime? FechaProxima { get; set; }

        public int? DiasRestantes { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public string CssClass { get; set; } = string.Empty;

        public string Icono { get; set; } = string.Empty;
    }
}