namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlMetrologicoScoreViewModel
    {
        public ControlMetrologicoFiltroViewModel Filtros { get; set; } = new();

        public ResumenScoreMetrologicoViewModel ResumenScore { get; set; } = new();

        public List<ScoreMetrologicoItemViewModel> Items { get; set; } = new();
    }
}
