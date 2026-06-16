namespace CGA.MetrologySystem.Models.TendenciasMetrologicas
{
    public class TendenciasMetrologicasDetalleViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string TipoEquipo { get; set; } = string.Empty;

        public TendenciasMetrologicasFiltroViewModel Filtros { get; set; } = new();

        public ResumenDesviacionesViewModel Resumen { get; set; } = new();

        public List<DesviacionHistoricaItemViewModel> Desviaciones { get; set; } = new();
    }
}
