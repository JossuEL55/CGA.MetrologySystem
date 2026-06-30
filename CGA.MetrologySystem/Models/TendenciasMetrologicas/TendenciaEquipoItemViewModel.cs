namespace CGA.MetrologySystem.Models.TendenciasMetrologicas
{
    public class TendenciaEquipoItemViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string TipoEquipo { get; set; } = string.Empty;

        public int EventosAnalizados { get; set; }

        public double DesviacionPromedio { get; set; }

        public int MayorDesviacion { get; set; }

        public int EventosTardios { get; set; }

        public int EventosExtraordinarios { get; set; }
    }
}
