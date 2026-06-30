namespace CGA.MetrologySystem.Models.Auditoria
{
    public class AuditoriaMetrologicaListadoViewModel
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string RolUsuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public string CodigoEquipo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public bool EsCritico { get; set; }
    }
}
