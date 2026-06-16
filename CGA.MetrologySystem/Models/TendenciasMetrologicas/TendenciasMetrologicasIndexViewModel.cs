namespace CGA.MetrologySystem.Models.TendenciasMetrologicas
{
    public class TendenciasMetrologicasIndexViewModel
    {
        public TendenciasMetrologicasFiltroViewModel Filtros { get; set; } = new();

        public ResumenDesviacionesViewModel Resumen { get; set; } = new();

        public List<TendenciaEquipoItemViewModel> Equipos { get; set; } = new();
    }
}
