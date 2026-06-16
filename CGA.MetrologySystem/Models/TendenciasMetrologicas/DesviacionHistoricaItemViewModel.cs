namespace CGA.MetrologySystem.Models.TendenciasMetrologicas
{
    public class DesviacionHistoricaItemViewModel
    {
        public int EventoMetrologicoId { get; set; }

        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string TipoEquipo { get; set; } = string.Empty;

        public int TipoEventoMetrologicoId { get; set; }

        public string TipoControl { get; set; } = string.Empty;

        public DateTime FechaEvento { get; set; }

        public DateTime FechaEsperada { get; set; }

        public DateTime FechaReal { get; set; }

        public int DesviacionDias { get; set; }

        public bool EsExtraordinario { get; set; }

        public string? JustificacionExtraordinario { get; set; }
    }
}
