namespace CGA.MetrologySystem.Application.DTOs.Rules
{
    public class ResultadoReglaMetrologicaDto
    {
        public bool EsValido { get; set; } = true;

        public bool TieneConfiguracion { get; set; }

        public bool RequiereControl { get; set; }

        public bool PermitePorIngreso { get; set; }

        public bool EsExtraordinario { get; set; }

        public DateTime? FechaUltimoEvento { get; set; }

        public DateTime? FechaEsperada { get; set; }

        public DateTime? FechaProximaCalculada { get; set; }

        public int? DiasDesviacion { get; set; }

        public string? Mensaje { get; set; }

        public string? Advertencia { get; set; }
    }
}