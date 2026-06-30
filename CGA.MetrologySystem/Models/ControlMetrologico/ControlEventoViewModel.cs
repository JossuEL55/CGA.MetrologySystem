namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlEventoViewModel
    {
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