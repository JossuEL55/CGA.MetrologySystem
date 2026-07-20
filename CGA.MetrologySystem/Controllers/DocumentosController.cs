using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.Documentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Controllers
{
    [Authorize(Roles = RolesSistema.TodosOperativos)]
    public class DocumentosController : Controller
    {
        private const string FichaTecnica = "ficha-tecnica";
        private const string HojaVida = "hoja-vida";
        private const string Mantenimiento = "mantenimiento";
        private const string Verificacion = "verificacion";
        private const string Calibracion = "calibracion";
        private const string EvidenciaEvento = "evidencia-evento";
        private const string EvidenciaMantenimientoItem = "evidencia-mantenimiento-item";
        private const string EvidenciaVerificacionItem = "evidencia-verificacion-item";
        private static readonly HashSet<char> CaracteresInvalidosNombreArchivo = new(
            Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*"));

        private readonly AppDbContext _context;
        private readonly IGoogleDriveService _googleDriveService;

        public DocumentosController(
            AppDbContext context,
            IGoogleDriveService googleDriveService)
        {
            _context = context;
            _googleDriveService = googleDriveService;
        }

        [HttpGet]
        public async Task<IActionResult> VerPdf(string tipo, int id, string? returnUrl = null)
        {
            var descriptor = await ObtenerDescriptorPdfAsync(tipo, id);

            if (descriptor == null)
                return NotFound();

            var model = new PdfViewerViewModel
            {
                Tipo = descriptor.Tipo,
                Id = descriptor.Id,
                Titulo = descriptor.Titulo,
                NombreArchivo = descriptor.NombreArchivo,
                EtiquetaDocumento = descriptor.EtiquetaDocumento,
                CodigoEquipo = descriptor.CodigoEquipo,
                NombreEquipo = descriptor.NombreEquipo,
                PdfUrl = Url.Action(nameof(ContenidoPdf), new { tipo = descriptor.Tipo, id = descriptor.Id }) ?? string.Empty,
                ReturnUrl = ObtenerReturnUrl(returnUrl, descriptor.UrlRetorno)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ContenidoPdf(string tipo, int id)
        {
            var descriptor = await ObtenerDescriptorPdfAsync(tipo, id);

            if (descriptor == null)
                return NotFound();

            var file = await _googleDriveService.DownloadFileAsync(descriptor.GoogleDriveFileId);
            var fileName = string.IsNullOrWhiteSpace(descriptor.NombreArchivo)
                ? file.FileName
                : descriptor.NombreArchivo;

            Response.Headers.CacheControl = "private, max-age=300";
            Response.Headers.ContentDisposition = $"inline; filename=\"{SanitizarNombreArchivo(fileName)}\"";

            return File(file.Content, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> VerImagen(string tipo, int id, string? returnUrl = null)
        {
            var descriptor = await ObtenerDescriptorImagenAsync(tipo, id);

            if (descriptor == null)
                return NotFound();

            var model = new ImageViewerViewModel
            {
                Tipo = descriptor.Tipo,
                Id = descriptor.Id,
                Titulo = descriptor.Titulo,
                NombreArchivo = descriptor.NombreArchivo,
                EtiquetaDocumento = descriptor.EtiquetaDocumento,
                CodigoEquipo = descriptor.CodigoEquipo,
                NombreEquipo = descriptor.NombreEquipo,
                ImagenUrl = Url.Action(nameof(ContenidoImagen), new { tipo = descriptor.Tipo, id = descriptor.Id }) ?? string.Empty,
                ReturnUrl = ObtenerReturnUrl(returnUrl, descriptor.UrlRetorno)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ContenidoImagen(string tipo, int id)
        {
            var descriptor = await ObtenerDescriptorImagenAsync(tipo, id);

            if (descriptor == null)
                return NotFound();

            var file = await _googleDriveService.DownloadFileAsync(descriptor.GoogleDriveFileId);
            var contentType = ObtenerContentTypeImagen(descriptor.ContentType, file.MimeType);
            var fileName = string.IsNullOrWhiteSpace(descriptor.NombreArchivo)
                ? file.FileName
                : descriptor.NombreArchivo;

            Response.Headers.CacheControl = "private, max-age=300";
            Response.Headers.ContentDisposition = $"inline; filename=\"{SanitizarNombreArchivo(fileName)}\"";

            return File(file.Content, contentType);
        }

        private async Task<PdfDocumentoDescriptor?> ObtenerDescriptorPdfAsync(string tipo, int id)
        {
            tipo = NormalizarTipo(tipo);

            return tipo switch
            {
                FichaTecnica => await ObtenerFichaTecnicaAsync(id),
                HojaVida => await ObtenerHojaVidaAsync(id),
                Mantenimiento => await ObtenerMantenimientoAsync(id),
                Verificacion => await ObtenerVerificacionAsync(id),
                Calibracion => await ObtenerCalibracionAsync(id),
                _ => null
            };
        }

        private async Task<ImagenDocumentoDescriptor?> ObtenerDescriptorImagenAsync(string tipo, int id)
        {
            tipo = NormalizarTipo(tipo);

            return tipo switch
            {
                EvidenciaEvento => await ObtenerEvidenciaEventoAsync(id),
                EvidenciaMantenimientoItem => await ObtenerEvidenciaMantenimientoItemAsync(id),
                EvidenciaVerificacionItem => await ObtenerEvidenciaVerificacionItemAsync(id),
                _ => null
            };
        }

        private async Task<PdfDocumentoDescriptor?> ObtenerFichaTecnicaAsync(int id)
        {
            var ficha = await _context.FichasTecnicasEquipo
                .AsNoTracking()
                .Include(f => f.Equipo)
                .FirstOrDefaultAsync(f => f.FichaTecnicaEquipoId == id && f.Activa);

            if (ficha == null || string.IsNullOrWhiteSpace(ficha.GoogleDriveFileId))
                return null;

            return new PdfDocumentoDescriptor(
                FichaTecnica,
                ficha.FichaTecnicaEquipoId,
                "Ficha tecnica",
                ficha.NombreArchivoPdf,
                CrearEtiqueta("Ficha tecnica", ficha.Equipo.Codigo, ficha.FechaUltimaGeneracion),
                ficha.GoogleDriveFileId,
                ficha.Equipo.Codigo,
                ficha.Equipo.Nombre,
                Url.Action("Index", "FichasTecnicas", new { equipoId = ficha.EquipoId }) ?? "/");
        }

        private async Task<PdfDocumentoDescriptor?> ObtenerHojaVidaAsync(int id)
        {
            var hojaVida = await _context.HojasVidaEquipo
                .AsNoTracking()
                .Include(h => h.Equipo)
                .FirstOrDefaultAsync(h => h.HojaVidaEquipoId == id && h.Activa);

            if (hojaVida == null || string.IsNullOrWhiteSpace(hojaVida.GoogleDriveFileId))
                return null;

            return new PdfDocumentoDescriptor(
                HojaVida,
                hojaVida.HojaVidaEquipoId,
                "Hoja de vida",
                hojaVida.NombreArchivoPdf,
                CrearEtiqueta("Hoja de vida", hojaVida.Equipo.Codigo, hojaVida.FechaUltimaGeneracion),
                hojaVida.GoogleDriveFileId,
                hojaVida.Equipo.Codigo,
                hojaVida.Equipo.Nombre,
                Url.Action("Index", "HojasVida", new { equipoId = hojaVida.EquipoId }) ?? "/");
        }

        private async Task<PdfDocumentoDescriptor?> ObtenerMantenimientoAsync(int id)
        {
            var mantenimiento = await _context.EventosMantenimientoDato
                .AsNoTracking()
                .Include(m => m.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(m => m.EventoMantenimientoDatoId == id);

            if (mantenimiento == null || string.IsNullOrWhiteSpace(mantenimiento.GoogleDriveFileId))
                return null;

            return new PdfDocumentoDescriptor(
                Mantenimiento,
                mantenimiento.EventoMantenimientoDatoId,
                "Mantenimiento",
                mantenimiento.NombreArchivoPdf ?? "mantenimiento.pdf",
                CrearEtiqueta("Certificado de mantenimiento", mantenimiento.EventoMetrologico.Equipo.Codigo, mantenimiento.EventoMetrologico.FechaEvento),
                mantenimiento.GoogleDriveFileId,
                mantenimiento.EventoMetrologico.Equipo.Codigo,
                mantenimiento.EventoMetrologico.Equipo.Nombre,
                Url.Action("Details", "Mantenimientos", new { id = mantenimiento.EventoMantenimientoDatoId }) ?? "/");
        }

        private async Task<PdfDocumentoDescriptor?> ObtenerVerificacionAsync(int id)
        {
            var verificacion = await _context.EventosVerificacionDato
                .AsNoTracking()
                .Include(v => v.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(v => v.EventoVerificacionDatoId == id);

            if (verificacion == null || string.IsNullOrWhiteSpace(verificacion.GoogleDriveFileId))
                return null;

            return new PdfDocumentoDescriptor(
                Verificacion,
                verificacion.EventoVerificacionDatoId,
                "Verificacion",
                verificacion.NombreArchivoPdf ?? "verificacion.pdf",
                CrearEtiqueta("Certificado de verificacion", verificacion.EventoMetrologico.Equipo.Codigo, verificacion.EventoMetrologico.FechaEvento),
                verificacion.GoogleDriveFileId,
                verificacion.EventoMetrologico.Equipo.Codigo,
                verificacion.EventoMetrologico.Equipo.Nombre,
                Url.Action("Details", "Verificaciones", new { id = verificacion.EventoVerificacionDatoId }) ?? "/");
        }

        private async Task<PdfDocumentoDescriptor?> ObtenerCalibracionAsync(int id)
        {
            var calibracion = await _context.EventosCalibracionDato
                .AsNoTracking()
                .Include(c => c.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(c => c.EventoCalibracionDatoId == id);

            if (calibracion == null || string.IsNullOrWhiteSpace(calibracion.GoogleDriveFileId))
                return null;

            return new PdfDocumentoDescriptor(
                Calibracion,
                calibracion.EventoCalibracionDatoId,
                "Certificado de calibracion",
                calibracion.NombreArchivoCertificado ?? "certificado-calibracion.pdf",
                CrearEtiqueta("Certificado de calibracion", calibracion.EventoMetrologico.Equipo.Codigo, calibracion.EventoMetrologico.FechaEvento),
                calibracion.GoogleDriveFileId,
                calibracion.EventoMetrologico.Equipo.Codigo,
                calibracion.EventoMetrologico.Equipo.Nombre,
                Url.Action("Details", "Calibraciones", new { id = calibracion.EventoCalibracionDatoId }) ?? "/");
        }

        private async Task<ImagenDocumentoDescriptor?> ObtenerEvidenciaEventoAsync(int id)
        {
            var evidencia = await _context.EvidenciasEventoMetrologico
                .AsNoTracking()
                .Include(e => e.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(e => e.EvidenciaEventoMetrologicoId == id && e.Activo);

            if (evidencia == null || string.IsNullOrWhiteSpace(evidencia.GoogleDriveFileId))
                return null;

            var retorno = await ObtenerUrlRetornoEventoAsync(evidencia.EventoMetrologicoId);

            return new ImagenDocumentoDescriptor(
                EvidenciaEvento,
                evidencia.EvidenciaEventoMetrologicoId,
                "Evidencia visual",
                evidencia.NombreArchivo,
                CrearEtiqueta("Evidencia visual", evidencia.EventoMetrologico.Equipo.Codigo, evidencia.EventoMetrologico.FechaEvento),
                evidencia.GoogleDriveFileId,
                evidencia.ContentType,
                evidencia.EventoMetrologico.Equipo.Codigo,
                evidencia.EventoMetrologico.Equipo.Nombre,
                retorno);
        }

        private async Task<ImagenDocumentoDescriptor?> ObtenerEvidenciaMantenimientoItemAsync(int id)
        {
            var actividad = await _context.EventosMantenimientoActividad
                .AsNoTracking()
                .Include(a => a.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(a => a.EventoMantenimientoActividadId == id);

            if (actividad == null || string.IsNullOrWhiteSpace(actividad.EvidenciaGoogleDriveFileId))
                return null;

            var mantenimientoId = await _context.EventosMantenimientoDato
                .AsNoTracking()
                .Where(m => m.EventoMetrologicoId == actividad.EventoMetrologicoId)
                .Select(m => (int?)m.EventoMantenimientoDatoId)
                .FirstOrDefaultAsync();

            return new ImagenDocumentoDescriptor(
                EvidenciaMantenimientoItem,
                actividad.EventoMantenimientoActividadId,
                "Evidencia de mantenimiento",
                actividad.EvidenciaNombreArchivo ?? "evidencia-mantenimiento",
                CrearEtiqueta("Evidencia de mantenimiento", actividad.EventoMetrologico.Equipo.Codigo, actividad.EventoMetrologico.FechaEvento),
                actividad.EvidenciaGoogleDriveFileId,
                actividad.EvidenciaContentType,
                actividad.EventoMetrologico.Equipo.Codigo,
                actividad.EventoMetrologico.Equipo.Nombre,
                mantenimientoId.HasValue
                    ? Url.Action("Details", "Mantenimientos", new { id = mantenimientoId.Value }) ?? "/"
                    : "/");
        }

        private async Task<ImagenDocumentoDescriptor?> ObtenerEvidenciaVerificacionItemAsync(int id)
        {
            var resultado = await _context.EventosVerificacionResultado
                .AsNoTracking()
                .Include(r => r.EventoMetrologico)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(r => r.EventoVerificacionResultadoId == id);

            if (resultado == null || string.IsNullOrWhiteSpace(resultado.EvidenciaGoogleDriveFileId))
                return null;

            var verificacionId = await _context.EventosVerificacionDato
                .AsNoTracking()
                .Where(v => v.EventoMetrologicoId == resultado.EventoMetrologicoId)
                .Select(v => (int?)v.EventoVerificacionDatoId)
                .FirstOrDefaultAsync();

            return new ImagenDocumentoDescriptor(
                EvidenciaVerificacionItem,
                resultado.EventoVerificacionResultadoId,
                "Evidencia de verificacion",
                resultado.EvidenciaNombreArchivo ?? "evidencia-verificacion",
                CrearEtiqueta("Evidencia de verificacion", resultado.EventoMetrologico.Equipo.Codigo, resultado.EventoMetrologico.FechaEvento),
                resultado.EvidenciaGoogleDriveFileId,
                resultado.EvidenciaContentType,
                resultado.EventoMetrologico.Equipo.Codigo,
                resultado.EventoMetrologico.Equipo.Nombre,
                verificacionId.HasValue
                    ? Url.Action("Details", "Verificaciones", new { id = verificacionId.Value }) ?? "/"
                    : "/");
        }

        private async Task<string> ObtenerUrlRetornoEventoAsync(int eventoMetrologicoId)
        {
            var mantenimientoId = await _context.EventosMantenimientoDato
                .AsNoTracking()
                .Where(m => m.EventoMetrologicoId == eventoMetrologicoId)
                .Select(m => (int?)m.EventoMantenimientoDatoId)
                .FirstOrDefaultAsync();

            if (mantenimientoId.HasValue)
                return Url.Action("Details", "Mantenimientos", new { id = mantenimientoId.Value }) ?? "/";

            var verificacionId = await _context.EventosVerificacionDato
                .AsNoTracking()
                .Where(v => v.EventoMetrologicoId == eventoMetrologicoId)
                .Select(v => (int?)v.EventoVerificacionDatoId)
                .FirstOrDefaultAsync();

            if (verificacionId.HasValue)
                return Url.Action("Details", "Verificaciones", new { id = verificacionId.Value }) ?? "/";

            var calibracionId = await _context.EventosCalibracionDato
                .AsNoTracking()
                .Where(c => c.EventoMetrologicoId == eventoMetrologicoId)
                .Select(c => (int?)c.EventoCalibracionDatoId)
                .FirstOrDefaultAsync();

            if (calibracionId.HasValue)
                return Url.Action("Details", "Calibraciones", new { id = calibracionId.Value }) ?? "/";

            return "/";
        }

        private string ObtenerReturnUrl(string? returnUrl, string fallbackUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return returnUrl;

            var referer = Request.Headers.Referer.ToString();

            if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) &&
                string.Equals(refererUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
                refererUri.Port == Request.Host.Port)
            {
                return refererUri.PathAndQuery;
            }

            return fallbackUrl;
        }

        private static string NormalizarTipo(string tipo)
        {
            return (tipo ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string SanitizarNombreArchivo(string fileName)
        {
            fileName = new string(fileName
                .Select(c => char.IsControl(c) || CaracteresInvalidosNombreArchivo.Contains(c)
                    ? '-'
                    : c)
                .ToArray());

            return string.IsNullOrWhiteSpace(fileName)
                ? "documento.pdf"
                : fileName;
        }

        private static string ObtenerContentTypeImagen(string? descriptorContentType, string? driveContentType)
        {
            var contentType = !string.IsNullOrWhiteSpace(descriptorContentType)
                ? descriptorContentType
                : driveContentType;

            return !string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? contentType
                : "image/jpeg";
        }

        private static string CrearEtiqueta(string titulo, string codigoEquipo, DateTime? fecha)
        {
            if (fecha.HasValue)
                return $"{titulo} - {codigoEquipo} - {fecha.Value:dd/MM/yyyy}";

            return $"{titulo} - {codigoEquipo}";
        }

        private record PdfDocumentoDescriptor(
            string Tipo,
            int Id,
            string Titulo,
            string NombreArchivo,
            string EtiquetaDocumento,
            string GoogleDriveFileId,
            string CodigoEquipo,
            string NombreEquipo,
            string UrlRetorno);

        private record ImagenDocumentoDescriptor(
            string Tipo,
            int Id,
            string Titulo,
            string NombreArchivo,
            string EtiquetaDocumento,
            string GoogleDriveFileId,
            string? ContentType,
            string CodigoEquipo,
            string NombreEquipo,
            string UrlRetorno);
    }
}
