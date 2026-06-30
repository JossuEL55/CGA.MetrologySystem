namespace CGA.MetrologySystem.Models.DashboardMetrologico
{
    public class DashboardMetrologicoViewModel
    {
        public DashboardMetrologicoFiltroViewModel Filtros { get; set; } = new();

        public ResumenDashboardMetrologicoViewModel Resumen { get; set; } = new();

        public List<GraficoEstadoItemViewModel> DistribucionEstados { get; set; } = new();

        public List<GraficoTipoControlItemViewModel> ControlesPorTipo { get; set; } = new();

        public List<GraficoVencimientoMensualItemViewModel> VencimientosPorMes { get; set; } = new();

        public List<GraficoScoreItemViewModel> TopScore { get; set; } = new();

        public List<ControlCriticoDashboardItemViewModel> ControlesCriticos { get; set; } = new();
    }
}
