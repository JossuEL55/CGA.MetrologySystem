using CGA.MetrologySystem.Models.ControlMetrologico;

namespace CGA.MetrologySystem.Models.MaestroEquipos
{
    public class MaestroEquipoControlItemViewModel
    {
        public int EquipoId { get; set; }

        public int TipoEventoMetrologicoId { get; set; }

        public int? ConfiguracionControlEquipoId { get; set; }

        public string TipoControl { get; set; } = string.Empty;

        public EstadoControlMetrologico Estado { get; set; }

        public string EstadoTexto { get; set; } = string.Empty;

        public string CssEstado { get; set; } = string.Empty;

        public string IconoEstado { get; set; } = string.Empty;

        public DateTime? FechaUltimoEvento { get; set; }

        public DateTime? FechaProxima { get; set; }

        public int? DiasRestantes { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public bool RequiereConfiguracion { get; set; }

        public bool NoRequiereControl { get; set; }

        public string? UrlConfigurar { get; set; }

        public string? UrlEditarConfiguracion { get; set; }

        public string? UrlRegistrarEvento { get; set; }
    }
}
