namespace CGA.MetrologySystem.Domain.Entities
{
    public class NotificacionSeguridadEnviada
    {
        public int NotificacionSeguridadEnviadaId { get; set; }
        public string TipoEvento { get; set; } = string.Empty;
        public string UsuarioAfectadoId { get; set; } = string.Empty;
        public string UsuarioAfectadoCorreo { get; set; } = string.Empty;
        public string? UsuarioEjecutorId { get; set; }
        public string? UsuarioEjecutorCorreo { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        public string? Destinatarios { get; set; }
        public string? Mensaje { get; set; }
        public bool FueExitosa { get; set; }
    }
}
