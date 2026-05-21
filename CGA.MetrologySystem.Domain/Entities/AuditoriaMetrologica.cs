namespace CGA.MetrologySystem.Domain.Entities
{
    public class AuditoriaMetrologica
    {
        public int AuditoriaMetrologicaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public string? UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioCorreo { get; set; } = string.Empty;
        public string RolUsuario { get; set; } = string.Empty;

        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public string EntidadId { get; set; } = string.Empty;

        public int? EquipoId { get; set; }
        public string CodigoEquipo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;

        public int? EventoMetrologicoId { get; set; }
        public string TipoEvento { get; set; } = string.Empty;

        public string Detalle { get; set; } = string.Empty;
        public bool EsCritico { get; set; }
    }
}
