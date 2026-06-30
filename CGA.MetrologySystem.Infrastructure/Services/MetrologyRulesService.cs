using CGA.MetrologySystem.Application.DTOs.Rules;
using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Infrastructure.Services
{
    public class MetrologyRulesService : IMetrologyRulesService
    {
        private readonly AppDbContext _context;

        private const int DiasPermitidosDesviacion = 20;

        public MetrologyRulesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoReglaMetrologicaDto> EvaluarEventoAsync(
            int equipoId,
            int tipoEventoMetrologicoId,
            DateTime fechaEvento,
            int subtipoEventoId,
            string? justificacionExtraordinario,
            bool esHistorico = false)
        {
            var resultado = new ResultadoReglaMetrologicaDto();

            var esSubtipoExtraordinario = await _context.SubtiposEvento
                .AsNoTracking()
                .AnyAsync(s =>
                    s.SubtipoEventoId == subtipoEventoId &&
                    s.Activo &&
                    s.Nombre == "Extraordinario");

            var configuracion = await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.EquipoId == equipoId &&
                    c.TipoEventoMetrologicoId == tipoEventoMetrologicoId &&
                    c.Activo);

            if (configuracion == null)
            {
                resultado.EsValido = false;
                resultado.TieneConfiguracion = false;
                resultado.Mensaje = "El equipo no tiene una configuración activa para este tipo de evento.";
                return resultado;
            }

            resultado.TieneConfiguracion = true;
            resultado.RequiereControl = configuracion.RequiereControl;
            resultado.PermitePorIngreso = configuracion.PermitePorIngreso;
            resultado.EsExtraordinario = esSubtipoExtraordinario;

            if (!configuracion.RequiereControl)
            {
                if (resultado.EsExtraordinario &&
                    string.IsNullOrWhiteSpace(justificacionExtraordinario))
                {
                    resultado.EsValido = false;
                    resultado.Mensaje =
                        "El evento es extraordinario y requiere una justificación.";
                    return resultado;
                }

                resultado.EsValido = true;
                resultado.Mensaje = "El equipo no requiere control para este tipo de evento.";
                return resultado;
            }

            if (!configuracion.PeriodicidadValor.HasValue ||
                configuracion.PeriodicidadValor.Value <= 0 ||
                string.IsNullOrWhiteSpace(configuracion.PeriodicidadUnidad))
            {
                resultado.EsValido = false;
                resultado.Mensaje = "La configuración del equipo no tiene una periodicidad válida.";
                return resultado;
            }

            var ultimoEvento = await _context.EventosMetrologicos
                .AsNoTracking()
                .Where(e =>
                    e.EquipoId == equipoId &&
                    e.TipoEventoMetrologicoId == tipoEventoMetrologicoId &&
                    e.Activo &&
                    e.FechaEvento < fechaEvento)
                .OrderByDescending(e => e.FechaEvento)
                .FirstOrDefaultAsync();

            resultado.FechaUltimoEvento = ultimoEvento?.FechaEvento;

            if (ultimoEvento != null)
            {
                resultado.FechaEsperada = CalcularFechaPorPeriodicidad(
                    ultimoEvento.FechaEvento,
                    configuracion.PeriodicidadValor.Value,
                    configuracion.PeriodicidadUnidad);

                if (!esHistorico)
                {
                    resultado.DiasDesviacion = Math.Abs(
                        (fechaEvento.Date - resultado.FechaEsperada.Value.Date).Days);

                    if (resultado.DiasDesviacion > DiasPermitidosDesviacion)
                    {
                        resultado.EsExtraordinario = true;
                        resultado.Advertencia =
                            $"El evento se desvía {resultado.DiasDesviacion} días de la fecha esperada.";

                        if (!esSubtipoExtraordinario)
                        {
                            resultado.EsValido = false;
                            resultado.Mensaje =
                                "La fecha registrada requiere clasificar el evento con el subtipo 'Extraordinario' y justificarlo.";
                        }
                    }
                }
            }

            if (resultado.EsValido &&
                resultado.EsExtraordinario &&
                string.IsNullOrWhiteSpace(justificacionExtraordinario))
            {
                resultado.EsValido = false;
                resultado.Mensaje =
                    "El evento es extraordinario y requiere una justificación.";
            }

            if (esSubtipoExtraordinario &&
                string.IsNullOrWhiteSpace(resultado.Advertencia))
            {
                resultado.Advertencia =
                    "El evento será registrado como extraordinario y quedará trazado con su justificación.";
            }

            resultado.FechaProximaCalculada = CalcularFechaPorPeriodicidad(
                fechaEvento,
                configuracion.PeriodicidadValor.Value,
                configuracion.PeriodicidadUnidad);

            if (resultado.EsValido && string.IsNullOrWhiteSpace(resultado.Mensaje))
            {
                resultado.Mensaje = "Evento evaluado correctamente.";
            }

            return resultado;
        }

        public async Task<DateTime?> CalcularProximaFechaAsync(
            int equipoId,
            int tipoEventoMetrologicoId,
            DateTime fechaEvento)
        {
            var configuracion = await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.EquipoId == equipoId &&
                    c.TipoEventoMetrologicoId == tipoEventoMetrologicoId &&
                    c.Activo &&
                    c.RequiereControl);

            if (configuracion == null ||
                !configuracion.PeriodicidadValor.HasValue ||
                configuracion.PeriodicidadValor.Value <= 0 ||
                string.IsNullOrWhiteSpace(configuracion.PeriodicidadUnidad))
            {
                return null;
            }

            return CalcularFechaPorPeriodicidad(
                fechaEvento,
                configuracion.PeriodicidadValor.Value,
                configuracion.PeriodicidadUnidad);
        }

        private static DateTime CalcularFechaPorPeriodicidad(
            DateTime fechaBase,
            int valor,
            string unidad)
        {
            return unidad.Trim().ToLower() switch
            {
                "dias" or "días" => fechaBase.Date.AddDays(valor),
                "meses" => fechaBase.Date.AddMonths(valor),
                "anios" or "años" => fechaBase.Date.AddYears(valor),
                _ => throw new InvalidOperationException($"Unidad de periodicidad no soportada: {unidad}")
            };
        }
    }
}
