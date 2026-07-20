namespace CGA.MetrologySystem.Services.Notificaciones
{
    public interface IDestinatariosNotificacionService
    {
        bool EsModoPreproduccion { get; }
        bool PermiteCorreosRegistrados { get; }

        Task<List<string>> ObtenerAdministradoresSistemaAsync();
        Task<List<string>> ObtenerAdministradoresMetrologicosAsync();
        Task<List<string>> ObtenerTodosAdministradoresAsync();
    }
}
