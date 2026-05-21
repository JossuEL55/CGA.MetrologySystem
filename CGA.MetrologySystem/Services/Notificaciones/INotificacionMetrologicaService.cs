namespace CGA.MetrologySystem.Services.Notificaciones
{
    public interface INotificacionMetrologicaService
    {
        Task NotificarEventoExtraordinarioAsync(int eventoMetrologicoId);

        Task NotificarReemplazoCertificadoCalibracionAsync(
            int eventoCalibracionDatoId,
            string? nombreCertificadoAnterior,
            string? nombreCertificadoNuevo,
            string? usuarioResponsable);

        Task NotificarEdicionCriticaVerificacionAsync(
            int eventoVerificacionDatoId,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos);

        Task NotificarEdicionCriticaMantenimientoAsync(
            int eventoMantenimientoDatoId,
            string? usuarioResponsable,
            IReadOnlyCollection<string> cambiosCriticos);
    }
}
