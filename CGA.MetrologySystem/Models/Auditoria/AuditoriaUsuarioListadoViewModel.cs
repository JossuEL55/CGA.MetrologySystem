namespace CGA.MetrologySystem.Models.Auditoria
{
    public class AuditoriaUsuarioListadoViewModel
    {
        public DateTime Fecha { get; set; }
        public string AdministradorCorreo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string UsuarioAfectadoCorreo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
    }
}