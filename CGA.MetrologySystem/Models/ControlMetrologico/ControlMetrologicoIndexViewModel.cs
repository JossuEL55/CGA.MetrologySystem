namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlMetrologicoIndexViewModel
    {
        public ControlMetrologicoFiltroViewModel Filtros { get; set; } = new();

        public ResumenControlMetrologicoViewModel Resumen { get; set; } = new();

        public List<ControlEquipoViewModel> Equipos { get; set; } = new();
    }
}