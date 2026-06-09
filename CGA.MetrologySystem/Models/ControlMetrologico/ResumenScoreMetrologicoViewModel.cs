namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ResumenScoreMetrologicoViewModel
    {
        public int TotalControlesEvaluados { get; set; }

        public int ControlesCriticos { get; set; }

        public int ControlesAltaPrioridad { get; set; }

        public double PromedioScore { get; set; }

        public int MayorScore { get; set; }

        public string EquipoMayorScore { get; set; } = string.Empty;
    }
}
