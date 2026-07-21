using System.Net;
using System.Net.Mail;
using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Alertas
{
    public class AlertaMetrologicaService : IAlertaMetrologicaService
    {
        private const string TipoAlertaVencido = "Vencido";
        private static readonly SemaphoreSlim _reintentoSemaphore = new(1, 1);
        private static readonly int[] DiasPreventivosCalibracionVerificacion = { 30, 15, 7, 4, 1 };

        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IDestinatariosNotificacionService _destinatariosService;
        private readonly AlertasSettings _alertasSettings;

        public AlertaMetrologicaService(
            AppDbContext context,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IDestinatariosNotificacionService destinatariosService,
            IOptions<AlertasSettings> alertasOptions)
        {
            _context = context;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _destinatariosService = destinatariosService;
            _alertasSettings = alertasOptions.Value;
        }

        public async Task<ResultadoProcesamientoAlertas> ProcesarAlertasAsync()
        {
            var resultado = new ResultadoProcesamientoAlertas();
            var reglas = CrearReglasPreventivas();

            var administradores = await _destinatariosService.ObtenerTodosAdministradoresAsync();

            foreach (var regla in reglas)
            {
                resultado.ReglasEvaluadas++;
                await ProcesarReglaPreventivaAsync(regla, administradores, resultado);
            }

            foreach (var regla in CrearReglasVencidas())
            {
                resultado.ReglasEvaluadas++;
                await ProcesarReglaVencidaAsync(regla, administradores, resultado);
            }

            return resultado;
        }

        private static List<ReglaAlertaPreventiva> CrearReglasPreventivas()
        {
            var reglas = new List<ReglaAlertaPreventiva>();

            foreach (var dias in DiasPreventivosCalibracionVerificacion)
            {
                reglas.Add(new ReglaAlertaPreventiva("calibr", "Calibracion", CrearTipoAlertaDias(dias), dias));
                reglas.Add(new ReglaAlertaPreventiva("verific", "Verificacion", CrearTipoAlertaDias(dias), dias));
            }

            reglas.Add(new ReglaAlertaPreventiva("manten", "Mantenimiento", CrearTipoAlertaDias(15), 15));

            return reglas;
        }

        private static List<ReglaAlertaVencida> CrearReglasVencidas()
        {
            return new List<ReglaAlertaVencida>
            {
                new("calibr", "Calibracion"),
                new("verific", "Verificacion"),
                new("manten", "Mantenimiento")
            };
        }

        private static string CrearTipoAlertaDias(int dias)
        {
            return dias == 1
                ? "1Dia"
                : $"{dias}Dias";
        }

        private async Task ProcesarReglaPreventivaAsync(
            ReglaAlertaPreventiva regla,
            List<string> administradores,
            ResultadoProcesamientoAlertas resultado)
        {
            var tipoEvento = await _context.TiposEventoMetrologico
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Nombre.ToLower().Contains(regla.ClaveTipoEvento));

            if (tipoEvento == null)
            {
                resultado.ReglasSinTipoEvento++;
                return;
            }

            var configuraciones = await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .Include(c => c.Equipo)
                    .ThenInclude(e => e.TipoEquipo)
                .Include(c => c.Equipo)
                    .ThenInclude(e => e.ResponsableInterno)
                .Where(c =>
                    c.Activo &&
                    c.RequiereControl &&
                    c.TipoEventoMetrologicoId == tipoEvento.TipoEventoMetrologicoId &&
                    c.Equipo.Activo)
                .ToListAsync();

            foreach (var configuracion in configuraciones)
            {
                resultado.ControlesEvaluados++;

                var ultimoEvento = await _context.EventosMetrologicos
                    .AsNoTracking()
                    .Where(e =>
                        e.Activo &&
                        e.EquipoId == configuracion.EquipoId &&
                        e.TipoEventoMetrologicoId == tipoEvento.TipoEventoMetrologicoId &&
                        e.FechaProxima.HasValue)
                    .OrderByDescending(e => e.FechaEvento)
                    .FirstOrDefaultAsync();

                if (ultimoEvento?.FechaProxima == null)
                {
                    continue;
                }

                var fechaReferencia = ultimoEvento.FechaProxima.Value.Date;
                var diasRestantes = (fechaReferencia - DateTime.Today).Days;

                if (diasRestantes != regla.DiasAntes)
                {
                    continue;
                }

                resultado.AlertasCandidatas++;

                var fechaReferenciaUtc = DateTime.SpecifyKind(fechaReferencia, DateTimeKind.Utc);
                var yaFueEnviada = !DebeReenviarDuplicadosPorPrueba() &&
                    await ExisteAlertaExitosaAsync(
                        configuracion.EquipoId,
                        regla.TipoEvento,
                        regla.TipoAlerta,
                        fechaReferenciaUtc);

                if (yaFueEnviada)
                {
                    resultado.OmitidasPorDuplicado++;
                    continue;
                }

                var destinatarios = ObtenerDestinatariosPreventivos(configuracion.Equipo, administradores);
                var asunto = $"Alerta de {regla.TipoEvento}: {configuracion.Equipo.Codigo} vence en {regla.DiasAntes} dias";
                var cuerpo = ConstruirCuerpoPreventivo(
                    configuracion.Equipo,
                    regla,
                    fechaReferencia,
                    diasRestantes);

                var envio = await EnviarYRegistrarAsync(
                    configuracion.EquipoId,
                    regla.TipoEvento,
                    regla.TipoAlerta,
                    fechaReferenciaUtc,
                    destinatarios,
                    asunto,
                    cuerpo);

                RegistrarResultadoEnvio(resultado, envio);
            }
        }

        private async Task ProcesarReglaVencidaAsync(
            ReglaAlertaVencida regla,
            List<string> administradores,
            ResultadoProcesamientoAlertas resultado)
        {
            var tipoEvento = await _context.TiposEventoMetrologico
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Nombre.ToLower().Contains(regla.ClaveTipoEvento));

            if (tipoEvento == null)
            {
                resultado.ReglasSinTipoEvento++;
                return;
            }

            var configuraciones = await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .Include(c => c.Equipo)
                    .ThenInclude(e => e.TipoEquipo)
                .Include(c => c.Equipo)
                    .ThenInclude(e => e.ResponsableInterno)
                .Where(c =>
                    c.Activo &&
                    c.RequiereControl &&
                    c.TipoEventoMetrologicoId == tipoEvento.TipoEventoMetrologicoId &&
                    c.Equipo.Activo)
                .ToListAsync();

            foreach (var configuracion in configuraciones)
            {
                resultado.ControlesEvaluados++;

                var ultimoEvento = await _context.EventosMetrologicos
                    .AsNoTracking()
                    .Where(e =>
                        e.Activo &&
                        e.EquipoId == configuracion.EquipoId &&
                        e.TipoEventoMetrologicoId == tipoEvento.TipoEventoMetrologicoId &&
                        e.FechaProxima.HasValue)
                    .OrderByDescending(e => e.FechaEvento)
                    .FirstOrDefaultAsync();

                if (ultimoEvento?.FechaProxima == null)
                {
                    continue;
                }

                var fechaReferencia = ultimoEvento.FechaProxima.Value.Date;
                var diasRestantes = (fechaReferencia - DateTime.Today).Days;

                if (diasRestantes >= 0)
                {
                    continue;
                }

                resultado.AlertasCandidatas++;

                var fechaReferenciaUtc = DateTime.SpecifyKind(fechaReferencia, DateTimeKind.Utc);
                var debeEnviar = DebeReenviarDuplicadosPorPrueba() ||
                    await DebeEnviarAlertaVencidaAsync(
                        configuracion.EquipoId,
                        regla.TipoEvento,
                        fechaReferenciaUtc);

                if (!debeEnviar)
                {
                    resultado.OmitidasPorDuplicado++;
                    continue;
                }

                var diasVencidos = Math.Abs(diasRestantes);
                var destinatarios = ObtenerDestinatariosCriticos(configuracion.Equipo, administradores);
                var asunto = $"Alerta critica: {regla.TipoEvento} vencida para {configuracion.Equipo.Codigo}";
                var cuerpo = ConstruirCuerpoVencido(
                    configuracion.Equipo,
                    regla,
                    fechaReferencia,
                    diasVencidos);

                var envio = await EnviarYRegistrarAsync(
                    configuracion.EquipoId,
                    regla.TipoEvento,
                    TipoAlertaVencido,
                    fechaReferenciaUtc,
                    destinatarios,
                    asunto,
                    cuerpo);

                RegistrarResultadoEnvio(resultado, envio);
            }
        }

        private async Task<bool> ExisteAlertaExitosaAsync(
            int equipoId,
            string tipoEvento,
            string tipoAlerta,
            DateTime fechaReferenciaUtc)
        {
            return await _context.AlertasEnviadas.AnyAsync(a =>
                a.EquipoId == equipoId &&
                a.TipoEvento == tipoEvento &&
                a.TipoAlerta == tipoAlerta &&
                a.FechaReferencia == fechaReferenciaUtc &&
                a.FueExitosa);
        }

        private async Task<bool> DebeEnviarAlertaVencidaAsync(
            int equipoId,
            string tipoEvento,
            DateTime fechaReferenciaUtc)
        {
            var ultimaFechaEnvio = await _context.AlertasEnviadas
                .AsNoTracking()
                .Where(a =>
                    a.EquipoId == equipoId &&
                    a.TipoEvento == tipoEvento &&
                    a.TipoAlerta == TipoAlertaVencido &&
                    a.FechaReferencia == fechaReferenciaUtc &&
                    a.FueExitosa)
                .MaxAsync(a => (DateTime?)a.FechaEnvio);

            if (!ultimaFechaEnvio.HasValue)
            {
                return true;
            }

            return ultimaFechaEnvio.Value <= DateTime.UtcNow.AddDays(-7);
        }

        private async Task<ResultadoEnvioAlerta> EnviarYRegistrarAsync(
            int equipoId,
            string tipoEvento,
            string tipoAlerta,
            DateTime fechaReferenciaUtc,
            List<string> destinatarios,
            string asunto,
            string cuerpoHtml)
        {
            var mensajeRegistro = asunto;
            var fueExitosa = false;

            try
            {
                if (!destinatarios.Any())
                {
                    mensajeRegistro = "No se envio la alerta porque no existen destinatarios validos.";
                    await RegistrarAlertaAsync(
                        equipoId,
                        tipoEvento,
                        tipoAlerta,
                        fechaReferenciaUtc,
                        destinatarios,
                        mensajeRegistro,
                        false);

                    return ResultadoEnvioAlerta.SinDestinatarios;
                }
                else
                {
                    await _emailService.EnviarCorreoAsync(destinatarios, asunto, cuerpoHtml);
                    fueExitosa = true;
                }
            }
            catch (Exception ex)
            {
                mensajeRegistro = $"Error al enviar alerta: {ex.Message}";
            }

            await RegistrarAlertaAsync(
                equipoId,
                tipoEvento,
                tipoAlerta,
                fechaReferenciaUtc,
                destinatarios,
                mensajeRegistro,
                fueExitosa);

            return fueExitosa
                ? ResultadoEnvioAlerta.Enviada
                : ResultadoEnvioAlerta.Error;
        }

        public async Task<ResultadoReintentoAlerta> ReintentarAlertaFallidaAsync(int alertaEnviadaId)
        {
            await _reintentoSemaphore.WaitAsync();

            try
            {
                var alerta = await _context.AlertasEnviadas
                    .Include(a => a.Equipo)
                        .ThenInclude(e => e.TipoEquipo)
                    .Include(a => a.Equipo)
                        .ThenInclude(e => e.ResponsableInterno)
                    .FirstOrDefaultAsync(a => a.AlertaEnviadaId == alertaEnviadaId);

                if (alerta == null)
                {
                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "La alerta seleccionada no existe."
                    };
                }

                if (alerta.FueExitosa)
                {
                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "La alerta ya fue enviada correctamente y no requiere reintento."
                    };
                }

                if (alerta.Equipo == null)
                {
                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "No se pudo reintentar la alerta porque no se encontro el equipo asociado."
                    };
                }

                var intentoAnterior = CrearResumenIntentoAnterior(alerta);
                var fechaReferenciaUtc = DateTime.SpecifyKind(alerta.FechaReferencia.Date, DateTimeKind.Utc);

                var yaExisteExitosa = await ExisteAlertaExitosaEquivalenteAsync(alerta, fechaReferenciaUtc);
                if (yaExisteExitosa)
                {
                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "Ya existe una alerta equivalente enviada correctamente. No se realizo un nuevo envio."
                    };
                }

                var configuracionActual = await ObtenerConfiguracionActualAsync(alerta);
                if (configuracionActual == null)
                {
                    alerta.FechaEnvio = DateTime.UtcNow;
                    alerta.Mensaje = LimitarMensaje(
                        $"No se pudo reintentar: el equipo no tiene una configuracion actual activa para este tipo de evento. {intentoAnterior}");
                    await _context.SaveChangesAsync();

                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "No se pudo reintentar la alerta porque no existe una configuracion actual activa para ese control."
                    };
                }

                var administradores = await _destinatariosService.ObtenerTodosAdministradoresAsync();
                var esVencida = EsAlertaVencida(alerta.TipoAlerta);
                var destinatarios = esVencida
                    ? ObtenerDestinatariosCriticos(alerta.Equipo, administradores)
                    : ObtenerDestinatariosPreventivos(alerta.Equipo, administradores);

                if (!destinatarios.Any())
                {
                    alerta.FechaEnvio = DateTime.UtcNow;
                    alerta.Destinatarios = string.Empty;
                    alerta.Mensaje = LimitarMensaje(
                        $"Reintento fallido: no se encontraron destinatarios validos. {intentoAnterior}");
                    await _context.SaveChangesAsync();

                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "No se pudo reintentar la alerta porque no existen destinatarios validos."
                    };
                }

                var fechaReferencia = alerta.FechaReferencia.Date;
                var asunto = CrearAsuntoReintento(alerta, esVencida);
                var cuerpo = ConstruirCuerpoReintento(alerta, fechaReferencia, esVencida);

                if (string.IsNullOrWhiteSpace(cuerpo))
                {
                    alerta.FechaEnvio = DateTime.UtcNow;
                    alerta.Destinatarios = string.Join(", ", destinatarios);
                    alerta.Mensaje = LimitarMensaje(
                        $"No se pudo reintentar: el tipo de alerta no es compatible con el reproceso manual. {intentoAnterior}");
                    await _context.SaveChangesAsync();

                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "No se pudo reconstruir el correo de la alerta seleccionada."
                    };
                }

                try
                {
                    await _emailService.EnviarCorreoAsync(destinatarios, asunto, cuerpo);

                    alerta.FueExitosa = true;
                    alerta.FechaEnvio = DateTime.UtcNow;
                    alerta.FechaReferencia = fechaReferenciaUtc;
                    alerta.Destinatarios = string.Join(", ", destinatarios);
                    alerta.Mensaje = LimitarMensaje(
                        $"Alerta reenviada correctamente con informacion actual disponible. {intentoAnterior}");
                    await _context.SaveChangesAsync();

                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = true,
                        Mensaje = "La alerta fallida fue reenviada correctamente."
                    };
                }
                catch (Exception ex)
                {
                    alerta.FueExitosa = false;
                    alerta.FechaEnvio = DateTime.UtcNow;
                    alerta.Destinatarios = string.Join(", ", destinatarios);
                    alerta.Mensaje = LimitarMensaje($"Reintento fallido: {ex.Message}. {intentoAnterior}");
                    await _context.SaveChangesAsync();

                    return new ResultadoReintentoAlerta
                    {
                        FueExitosa = false,
                        Mensaje = "El reintento de la alerta fallo. Revise el mensaje registrado en la bitacora."
                    };
                }
            }
            finally
            {
                _reintentoSemaphore.Release();
            }
        }
        private async Task RegistrarAlertaAsync(
            int equipoId,
            string tipoEvento,
            string tipoAlerta,
            DateTime fechaReferenciaUtc,
            List<string> destinatarios,
            string mensajeRegistro,
            bool fueExitosa)
        {
            _context.AlertasEnviadas.Add(new AlertaEnviada
            {
                EquipoId = equipoId,
                TipoEvento = tipoEvento,
                TipoAlerta = tipoAlerta,
                FechaReferencia = fechaReferenciaUtc,
                FechaEnvio = DateTime.UtcNow,
                Destinatarios = string.Join(", ", destinatarios),
                Mensaje = mensajeRegistro,
                FueExitosa = fueExitosa
            });

            await _context.SaveChangesAsync();
        }

        private static void RegistrarResultadoEnvio(
            ResultadoProcesamientoAlertas resultado,
            ResultadoEnvioAlerta envio)
        {
            switch (envio)
            {
                case ResultadoEnvioAlerta.Enviada:
                    resultado.Enviadas++;
                    break;
                case ResultadoEnvioAlerta.SinDestinatarios:
                    resultado.SinDestinatarios++;
                    break;
                case ResultadoEnvioAlerta.Error:
                    resultado.Errores++;
                    break;
            }
        }

        private bool DebeReenviarDuplicadosPorPrueba()
        {
            return _destinatariosService.EsModoPreproduccion &&
                _alertasSettings.ReenviarDuplicadosEnModoPrueba;
        }

        private async Task<bool> ExisteAlertaExitosaEquivalenteAsync(
            AlertaEnviada alerta,
            DateTime fechaReferenciaUtc)
        {
            return await _context.AlertasEnviadas.AnyAsync(a =>
                a.AlertaEnviadaId != alerta.AlertaEnviadaId &&
                a.EquipoId == alerta.EquipoId &&
                a.TipoEvento == alerta.TipoEvento &&
                a.TipoAlerta == alerta.TipoAlerta &&
                a.FechaReferencia == fechaReferenciaUtc &&
                a.FueExitosa);
        }

        private async Task<ConfiguracionControlEquipo?> ObtenerConfiguracionActualAsync(AlertaEnviada alerta)
        {
            var claveTipoEvento = ObtenerClaveTipoEvento(alerta.TipoEvento);
            if (string.IsNullOrWhiteSpace(claveTipoEvento))
            {
                return null;
            }

            return await _context.ConfiguracionesControlEquipo
                .AsNoTracking()
                .Include(c => c.TipoEventoMetrologico)
                .Where(c =>
                    c.Activo &&
                    c.RequiereControl &&
                    c.EquipoId == alerta.EquipoId &&
                    c.TipoEventoMetrologico.Nombre.ToLower().Contains(claveTipoEvento))
                .FirstOrDefaultAsync();
        }

        private static string? ObtenerClaveTipoEvento(string tipoEvento)
        {
            var valor = tipoEvento.Trim().ToLowerInvariant();

            if (valor.Contains("calibr"))
            {
                return "calibr";
            }

            if (valor.Contains("verific"))
            {
                return "verific";
            }

            if (valor.Contains("manten"))
            {
                return "manten";
            }

            return null;
        }

        private static bool EsAlertaVencida(string tipoAlerta)
        {
            return string.Equals(tipoAlerta, TipoAlertaVencido, StringComparison.OrdinalIgnoreCase);
        }

        private static int? ObtenerDiasPreventivos(string tipoAlerta)
        {
            if (string.Equals(tipoAlerta, "1Dia", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (tipoAlerta.EndsWith("Dias", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(tipoAlerta[..^4], out var dias))
            {
                return dias;
            }

            return null;
        }

        private static string CrearAsuntoReintento(AlertaEnviada alerta, bool esVencida)
        {
            if (esVencida)
            {
                return $"Alerta critica: {alerta.TipoEvento} vencida para {alerta.Equipo.Codigo}";
            }

            var dias = ObtenerDiasPreventivos(alerta.TipoAlerta);
            return dias.HasValue
                ? $"Alerta de {alerta.TipoEvento}: {alerta.Equipo.Codigo} vence en {dias.Value} dias"
                : $"Alerta de {alerta.TipoEvento}: {alerta.Equipo.Codigo}";
        }

        private string? ConstruirCuerpoReintento(
            AlertaEnviada alerta,
            DateTime fechaReferencia,
            bool esVencida)
        {
            if (esVencida)
            {
                var diasVencidos = Math.Max(0, (DateTime.Today - fechaReferencia).Days);
                var regla = new ReglaAlertaVencida(ObtenerClaveTipoEvento(alerta.TipoEvento) ?? string.Empty, alerta.TipoEvento);
                return ConstruirCuerpoVencido(alerta.Equipo, regla, fechaReferencia, diasVencidos);
            }

            var diasPreventivos = ObtenerDiasPreventivos(alerta.TipoAlerta);
            if (!diasPreventivos.HasValue)
            {
                return null;
            }

            var diasRestantes = (fechaReferencia - DateTime.Today).Days;
            var reglaPreventiva = new ReglaAlertaPreventiva(
                ObtenerClaveTipoEvento(alerta.TipoEvento) ?? string.Empty,
                alerta.TipoEvento,
                alerta.TipoAlerta,
                diasPreventivos.Value);

            return ConstruirCuerpoPreventivo(alerta.Equipo, reglaPreventiva, fechaReferencia, diasRestantes);
        }

        private static string CrearResumenIntentoAnterior(AlertaEnviada alerta)
        {
            var fechaAnterior = alerta.FechaEnvio.ToString("yyyy-MM-dd HH:mm");
            var mensajeAnterior = string.IsNullOrWhiteSpace(alerta.Mensaje)
                ? "Sin mensaje previo."
                : alerta.Mensaje;

            return $"Intento anterior ({fechaAnterior} UTC): {mensajeAnterior}";
        }

        private static string LimitarMensaje(string mensaje)
        {
            return mensaje.Length <= 500
                ? mensaje
                : mensaje[..500];
        }
        private List<string> ObtenerDestinatariosPreventivos(
            Equipo equipo,
            List<string> administradores)
        {
            var destinatarios = new List<string>();
            var correoResponsable = equipo.ResponsableInterno?.Correo;

            if (_destinatariosService.PermiteCorreosRegistrados &&
                EsCorreoValido(correoResponsable))
            {
                destinatarios.Add(correoResponsable!.Trim().ToLower());
            }

            if (!destinatarios.Any())
            {
                destinatarios.AddRange(administradores);
            }

            return destinatarios
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<string> ObtenerDestinatariosCriticos(
            Equipo equipo,
            List<string> administradores)
        {
            var destinatarios = new List<string>();
            var correoResponsable = equipo.ResponsableInterno?.Correo;

            if (_destinatariosService.PermiteCorreosRegistrados &&
                EsCorreoValido(correoResponsable))
            {
                destinatarios.Add(correoResponsable!.Trim().ToLower());
            }

            destinatarios.AddRange(administradores);

            return destinatarios
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string ConstruirCuerpoPreventivo(
            Equipo equipo,
            ReglaAlertaPreventiva regla,
            DateTime fechaReferencia,
            int diasRestantes)
        {
            var codigo = WebUtility.HtmlEncode(equipo.Codigo);
            var nombre = WebUtility.HtmlEncode(equipo.Nombre);
            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Equipo", $"{codigo} - {nombre}"),
                new EmailTemplateRow("Tipo de equipo", equipo.TipoEquipo?.Nombre ?? "Sin tipo"),
                new EmailTemplateRow("Control", regla.TipoEvento),
                new EmailTemplateRow("Fecha programada", fechaReferencia.ToString("yyyy-MM-dd")),
                new EmailTemplateRow("Días restantes", diasRestantes.ToString()),
                new EmailTemplateRow("Responsable interno", equipo.ResponsableInterno?.NombreCompleto ?? "Sin responsable asignado")
            });

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = $"Alerta preventiva de {regla.TipoEvento}",
                Preheader = $"El equipo {equipo.Codigo} tiene una fecha crítica próxima.",
                Etiqueta = "Control metrológico",
                Nivel = "advertencia",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">El equipo <strong>{codigo} - {nombre}</strong> tiene una fecha crítica próxima.</p>
                    {tabla}
                    <p style=""margin:0;"">Por favor planificar la gestión correspondiente para mantener la trazabilidad metrológica del equipo.</p>"
            });
        }

        private string ConstruirCuerpoVencido(
            Equipo equipo,
            ReglaAlertaVencida regla,
            DateTime fechaReferencia,
            int diasVencidos)
        {
            var codigo = WebUtility.HtmlEncode(equipo.Codigo);
            var nombre = WebUtility.HtmlEncode(equipo.Nombre);
            var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
            {
                new EmailTemplateRow("Equipo", $"{codigo} - {nombre}"),
                new EmailTemplateRow("Tipo de equipo", equipo.TipoEquipo?.Nombre ?? "Sin tipo"),
                new EmailTemplateRow("Control", regla.TipoEvento),
                new EmailTemplateRow("Fecha programada", fechaReferencia.ToString("yyyy-MM-dd")),
                new EmailTemplateRow("Días vencidos", diasVencidos.ToString()),
                new EmailTemplateRow("Responsable interno", equipo.ResponsableInterno?.NombreCompleto ?? "Sin responsable asignado")
            });

            return _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
            {
                Titulo = $"Alerta crítica de {regla.TipoEvento} vencida",
                Preheader = $"El equipo {equipo.Codigo} tiene un control metrológico vencido.",
                Etiqueta = "Control metrológico",
                Nivel = "critico",
                ContenidoHtml = $@"
                    <p style=""margin:0 0 14px;"">El equipo <strong>{codigo} - {nombre}</strong> tiene un control metrológico vencido.</p>
                    {tabla}
                    <p style=""margin:0;"">Se requiere gestionar este control para reducir el riesgo operativo y mantener la trazabilidad del sistema.</p>"
            });
        }

        private static bool EsCorreoValido(string? correo)
        {
            return !string.IsNullOrWhiteSpace(correo) &&
                MailAddress.TryCreate(correo.Trim(), out _);
        }

        private sealed record ReglaAlertaPreventiva(
            string ClaveTipoEvento,
            string TipoEvento,
            string TipoAlerta,
            int DiasAntes);

        private sealed record ReglaAlertaVencida(
            string ClaveTipoEvento,
            string TipoEvento);

        private enum ResultadoEnvioAlerta
        {
            Enviada,
            SinDestinatarios,
            Error
        }
    }
}
