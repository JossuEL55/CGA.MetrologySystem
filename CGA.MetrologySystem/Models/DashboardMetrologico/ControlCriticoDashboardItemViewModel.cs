namespace CGA.MetrologySystem.Models.DashboardMetrologico
{
    public class ControlCriticoDashboardItemViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string TipoEquipo { get; set; } = string.Empty;

        public string TipoControl { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateTime? FechaProxima { get; set; }

        public int? DiasRestantes { get; set; }

        public int ScoreMetrologico { get; set; }

        public string NivelPrioridad { get; set; } = string.Empty;

        public string CssPrioridad { get; set; } = string.Empty;
    }
}
