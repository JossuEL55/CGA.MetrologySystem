using CGA.MetrologySystem.Models.ControlMetrologico;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CGA.MetrologySystem.Models.MaestroEquipos
{
    public class MaestroEquiposFiltroViewModel
    {
        public string? Buscar { get; set; }

        public int? TipoEquipoId { get; set; }

        public EstadoControlMetrologico? EstadoGlobal { get; set; }

        public bool SoloConfiguracionIncompleta { get; set; }

        public int HorizonteDias { get; set; } = 30;

        public List<SelectListItem> TiposEquipo { get; set; } = new();

        public List<SelectListItem> EstadosGlobales { get; set; } = new();
    }
}
