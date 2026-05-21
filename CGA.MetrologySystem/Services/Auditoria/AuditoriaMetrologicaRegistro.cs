namespace CGA.MetrologySystem.Services.Auditoria
{
    public class AuditoriaMetrologicaRegistro
    {
        public string? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? UsuarioCorreo { get; set; }
        public string? RolUsuario { get; set; }

        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public string EntidadId { get; set; } = string.Empty;

        public int? EquipoId { get; set; }
        public string? CodigoEquipo { get; set; }
        public string? NombreEquipo { get; set; }

        public int? EventoMetrologicoId { get; set; }
        public string? TipoEvento { get; set; }

        public string Detalle { get; set; } = string.Empty;
        public bool EsCritico { get; set; }
    }
}
