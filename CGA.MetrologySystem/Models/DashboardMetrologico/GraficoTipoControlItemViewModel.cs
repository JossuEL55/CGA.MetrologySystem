namespace CGA.MetrologySystem.Models.DashboardMetrologico
{
    public class GraficoTipoControlItemViewModel
    {
        public string TipoControl { get; set; } = string.Empty;

        public int Vigentes { get; set; }

        public int ProximosAVencer { get; set; }

        public int Vencidos { get; set; }

        public int SinEventos { get; set; }

        public int SinConfiguracion { get; set; }
    }
}
