using Microsoft.AspNetCore.Mvc.Rendering;

namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlMetrologicoFiltroViewModel
    {
        public string? Buscar { get; set; }

        public int? TipoEquipoId { get; set; }

        public int? TipoEventoMetrologicoId { get; set; }

        public EstadoControlMetrologico? Estado { get; set; }

        public int HorizonteDias { get; set; } = 30;

        public List<SelectListItem> TiposEquipo { get; set; } = new();

        public List<SelectListItem> TiposEvento { get; set; } = new();

        public List<SelectListItem> Estados { get; set; } = new();

        public List<SelectListItem> Horizontes { get; set; } = new();
    }
}