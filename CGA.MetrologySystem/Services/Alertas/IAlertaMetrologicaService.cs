namespace CGA.MetrologySystem.Services.Alertas
{
    public interface IAlertaMetrologicaService
    {
        Task<ResultadoProcesamientoAlertas> ProcesarAlertasAsync();
        Task<ResultadoReintentoAlerta> ReintentarAlertaFallidaAsync(int alertaEnviadaId);
    }

    public class ResultadoReintentoAlerta
    {
        public bool FueExitosa { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
