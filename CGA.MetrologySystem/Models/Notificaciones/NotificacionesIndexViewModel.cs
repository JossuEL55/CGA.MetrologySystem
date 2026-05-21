namespace CGA.MetrologySystem.Models.Notificaciones
{
    public class NotificacionesIndexViewModel
    {
        public int TotalNotificaciones { get; set; }
        public int NotificacionesExitosas { get; set; }
        public int NotificacionesFallidas { get; set; }
        public int NotificacionesUltimas24Horas { get; set; }
        public List<NotificacionEnviadaListadoViewModel> Notificaciones { get; set; } = new();
    }
}
