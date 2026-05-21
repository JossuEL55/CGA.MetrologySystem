using CGA.MetrologySystem.Configuration;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Alertas
{
    public class AlertasBackgroundService : BackgroundService
    {
        private static readonly TimeSpan HoraEjecucionPredeterminada = new(8, 0, 0);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<AlertasSettings> _alertasOptions;
        private readonly ILogger<AlertasBackgroundService> _logger;

        public AlertasBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<AlertasSettings> alertasOptions,
            ILogger<AlertasBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _alertasOptions = alertasOptions;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = _alertasOptions.CurrentValue;

                if (!settings.AutomatizacionHabilitada)
                {
                    await EsperarRevisionAsync(settings, stoppingToken);
                    continue;
                }

                var proximaEjecucion = CalcularProximaEjecucion(settings);
                var espera = proximaEjecucion - DateTime.Now;

                _logger.LogInformation(
                    "La proxima ejecucion automatica de alertas sera {FechaEjecucion}.",
                    proximaEjecucion);

                await Task.Delay(espera, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await EjecutarAlertasAsync(stoppingToken);
            }
        }

        private async Task EjecutarAlertasAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var alertaService = scope.ServiceProvider
                    .GetRequiredService<IAlertaMetrologicaService>();

                var resultado = await alertaService.ProcesarAlertasAsync();

                _logger.LogInformation(
                    "Proceso automatico de alertas finalizado. {Resumen}",
                    resultado.CrearMensajeResumen());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // La aplicacion se esta deteniendo.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo el proceso automatico de alertas metrologicas.");
            }
        }

        private static DateTime CalcularProximaEjecucion(AlertasSettings settings)
        {
            var hora = ObtenerHoraEjecucion(settings);
            var ahora = DateTime.Now;
            var ejecucionHoy = ahora.Date.Add(hora);

            return ejecucionHoy > ahora
                ? ejecucionHoy
                : ejecucionHoy.AddDays(1);
        }

        private static TimeSpan ObtenerHoraEjecucion(AlertasSettings settings)
        {
            return TimeSpan.TryParse(settings.HoraEjecucionDiaria, out var hora)
                ? hora
                : HoraEjecucionPredeterminada;
        }

        private static Task EsperarRevisionAsync(
            AlertasSettings settings,
            CancellationToken stoppingToken)
        {
            var minutos = Math.Max(settings.MinutosRevisionAutomatizacion, 1);
            return Task.Delay(TimeSpan.FromMinutes(minutos), stoppingToken);
        }
    }
}
