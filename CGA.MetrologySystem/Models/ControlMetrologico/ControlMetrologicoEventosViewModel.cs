namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlMetrologicoEventosViewModel
    {
        public ControlMetrologicoFiltroViewModel Filtros { get; set; } = new();

        public ResumenControlMetrologicoViewModel Resumen { get; set; } = new();

        public List<ControlEventoOperativoViewModel> Eventos { get; set; } = new();
    }
}