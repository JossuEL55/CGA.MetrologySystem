namespace CGA.MetrologySystem.Domain.Entities
{
    //Entidad para auditar los cambios de usuario
    public class AuditoriaUsuario
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public string AdministradorId { get; set; } = string.Empty;
        public string AdministradorCorreo { get; set; } = string.Empty;

        public string Accion { get; set; } = string.Empty;

        public string UsuarioAfectadoId { get; set; } = string.Empty;
        public string UsuarioAfectadoCorreo { get; set; } = string.Empty;

        public string Detalle { get; set; } = string.Empty;
    }
}