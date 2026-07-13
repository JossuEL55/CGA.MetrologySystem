namespace CGA.MetrologySystem.Models.Usuarios
{
    public class UsuariosIndexViewModel
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosInactivos { get; set; }
        public int TotalAdministradores { get; set; }
        public int TotalAdministradoresMetrologicos { get; set; }
        public int TotalTecnicos { get; set; }
        public string? Busqueda { get; set; }
        public string? Rol { get; set; }
        public string? Estado { get; set; }
        public List<UsuarioListadoViewModel> Usuarios { get; set; } = new();
    }
}
