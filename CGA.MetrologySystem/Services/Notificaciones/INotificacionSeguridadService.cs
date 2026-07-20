using CGA.MetrologySystem.Infrastructure.Identity;

namespace CGA.MetrologySystem.Services.Notificaciones
{
    public interface INotificacionSeguridadService
    {
        Task<bool> NotificarCambioContrasenaAsync(UsuarioSistema usuario);

        Task<bool> NotificarRestablecimientoContrasenaAsync(
            UsuarioSistema usuarioAfectado,
            UsuarioSistema? usuarioEjecutor);
    }
}
