namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ControlEquipoViewModel
    {
        public int EquipoId { get; set; }

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public int? TipoEquipoId { get; set; }

        public string TipoEquipo { get; set; } = string.Empty;

        public EstadoControlMetrologico EstadoGlobal { get; set; }

        public string EstadoGlobalTexto { get; set; } = string.Empty;

        public string EstadoGlobalCssClass { get; set; } = string.Empty;

        public string EstadoGlobalIcono { get; set; } = string.Empty;

        public ControlEventoViewModel Calibracion { get; set; } = new();

        public ControlEventoViewModel Verificacion { get; set; } = new();

        public ControlEventoViewModel Mantenimiento { get; set; } = new();
    }
}