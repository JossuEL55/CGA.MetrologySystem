namespace CGA.MetrologySystem.Models.DashboardMetrologico
{
    public class ResumenDashboardMetrologicoViewModel
    {
        public int TotalEquipos { get; set; }

        public int TotalControlesEvaluados { get; set; }

        public int EquiposVigentes { get; set; }

        public int EquiposProximosAVencer { get; set; }

        public int EquiposVencidos { get; set; }

        public int ControlesVencidos { get; set; }

        public int ControlesProximosAVencer { get; set; }

        public int ControlesCriticosScore { get; set; }

        public double PorcentajeCumplimiento { get; set; }

        public double PromedioScore { get; set; }
    }
}
