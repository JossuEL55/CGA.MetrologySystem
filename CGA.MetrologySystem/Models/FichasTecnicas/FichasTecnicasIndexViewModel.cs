using Microsoft.AspNetCore.Mvc.Rendering;

namespace CGA.MetrologySystem.Models.FichasTecnicas
{
    public class FichasTecnicasIndexViewModel
    {
        public string? Buscar { get; set; }

        public int? EquipoId { get; set; }

        public List<SelectListItem> Equipos { get; set; } = new();

        public List<FichaTecnicaEquipoItemViewModel> Fichas { get; set; } = new();
    }
}