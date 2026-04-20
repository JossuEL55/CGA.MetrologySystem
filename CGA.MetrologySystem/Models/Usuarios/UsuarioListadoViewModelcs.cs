namespace CGA.MetrologySystem.Models.Usuarios
{
    public class UsuarioListadoViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string Rol { get; set; } = string.Empty;
    }
}