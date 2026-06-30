using Microsoft.AspNetCore.Mvc.Rendering;

namespace CGA.MetrologySystem.Models.HojasVida
{
    public class HojasVidaIndexViewModel
    {
        public string? Buscar { get; set; }

        public int? EquipoId { get; set; }

        public List<SelectListItem> Equipos { get; set; } = new();

        public List<HojaVidaEquipoItemViewModel> HojasVida { get; set; } = new();
    }
}