using CGA.MetrologySystem.Application.DTOs.Rules;

namespace CGA.MetrologySystem.Application.Interfaces
{
    public interface IMetrologyRulesService
    {
        Task<ResultadoReglaMetrologicaDto> EvaluarEventoAsync(
            int equipoId,
            int tipoEventoMetrologicoId,
            DateTime fechaEvento,
            int subtipoEventoId,
            string? justificacionExtraordinario,
            bool esHistorico = false);

        Task<DateTime?> CalcularProximaFechaAsync(
            int equipoId,
            int tipoEventoMetrologicoId,
            DateTime fechaEvento);
    }
}
