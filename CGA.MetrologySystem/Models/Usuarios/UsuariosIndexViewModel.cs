namespace CGA.MetrologySystem.Models.Usuarios
{
    public class UsuariosIndexViewModel
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosInactivos { get; set; }
        public int TotalAdministradores { get; set; }
        public List<UsuarioListadoViewModel> Usuarios { get; set; } = new();
    }
}