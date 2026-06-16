using Microsoft.AspNetCore.Mvc.Rendering;

namespace CGA.MetrologySystem.Models.TendenciasMetrologicas
{
    public class TendenciasMetrologicasFiltroViewModel
    {
        public string? Buscar { get; set; }

        public int? TipoEquipoId { get; set; }

        public int? TipoEventoMetrologicoId { get; set; }

        public DateTime? FechaDesde { get; set; }

        public DateTime? FechaHasta { get; set; }

        public List<SelectListItem> TiposEquipo { get; set; } = new();

        public List<SelectListItem> TiposEvento { get; set; } = new();
    }
}
