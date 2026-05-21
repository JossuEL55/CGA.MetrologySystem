namespace CGA.MetrologySystem.Models.Alertas
{
    public class AlertasIndexViewModel
    {
        public int TotalAlertas { get; set; }
        public int AlertasExitosas { get; set; }
        public int AlertasFallidas { get; set; }
        public int AlertasUltimas24Horas { get; set; }
        public List<AlertaEnviadaListadoViewModel> Alertas { get; set; } = new();
    }
}
