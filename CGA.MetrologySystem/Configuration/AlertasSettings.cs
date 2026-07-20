namespace CGA.MetrologySystem.Configuration
{
    public class AlertasSettings
    {
        public bool ReenviarDuplicadosEnModoPrueba { get; set; }
        public bool AutomatizacionHabilitada { get; set; }
        public string HoraEjecucionDiaria { get; set; } = "08:00";
        public int MinutosRevisionAutomatizacion { get; set; } = 15;
    }
}
