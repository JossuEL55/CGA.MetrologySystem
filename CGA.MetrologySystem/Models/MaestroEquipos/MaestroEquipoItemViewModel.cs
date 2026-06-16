using CGA.MetrologySystem.Models.ControlMetrologico;

namespace CGA.MetrologySystem.Models.MaestroEquipos
{
    public class MaestroEquipoItemViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public int? TipoEquipoId { get; set; }

        public string TipoEquipo { get; set; } = string.Empty;

        public EstadoControlMetrologico EstadoGlobal { get; set; }

        public string EstadoGlobalTexto { get; set; } = string.Empty;

        public string CssEstadoGlobal { get; set; } = string.Empty;

        public string IconoEstadoGlobal { get; set; } = string.Empty;

        public int? ScoreMaximo { get; set; }

        public string? PrioridadMaxima { get; set; }

        public string? CssPrioridadMaxima { get; set; }

        public bool TieneConfiguracionIncompleta { get; set; }

        public List<MaestroEquipoControlItemViewModel> Controles { get; set; } = new();

        public List<MaestroEquipoAccionViewModel> Acciones { get; set; } = new();
    }
}
