namespace CGA.MetrologySystem.Services.Alertas
{
    public interface IAlertaMetrologicaService
    {
        Task<ResultadoProcesamientoAlertas> ProcesarAlertasAsync();
    }
}
