namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ScoreMetrologicoItemViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public int? TipoEquipoId { get; set; }

        public string TipoEquipo { get; set; } = string.Empty;

        public int TipoEventoMetrologicoId { get; set; }

        public string TipoEventoNombre { get; set; } = string.Empty;

        public EstadoControlMetrologico Estado { get; set; }

        public string EstadoTexto { get; set; } = string.Empty;

        public string CssEstado { get; set; } = string.Empty;

        public string IconoEstado { get; set; } = string.Empty;

        public DateTime? FechaUltimoEvento { get; set; }

        public DateTime? FechaProxima { get; set; }

        public int? DiasRestantes { get; set; }

        public int CantidadEventosExtraordinarios { get; set; }

        public int ScoreMetrologico { get; set; }

        public string NivelPrioridad { get; set; } = string.Empty;

        public string CssPrioridad { get; set; } = string.Empty;

        public string ExplicacionScore { get; set; } = string.Empty;
    }
}
