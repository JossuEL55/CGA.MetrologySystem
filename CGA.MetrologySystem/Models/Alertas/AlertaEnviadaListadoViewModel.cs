namespace CGA.MetrologySystem.Models.Alertas
{
    public class AlertaEnviadaListadoViewModel
    {
        public int AlertaEnviadaId { get; set; }
        public string CodigoEquipo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;
        public string TipoAlerta { get; set; } = string.Empty;
        public DateTime FechaReferencia { get; set; }
        public DateTime FechaEnvio { get; set; }
        public string Destinatarios { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool FueExitosa { get; set; }
        public bool PuedeReintentar { get; set; }
    }
}
