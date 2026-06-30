using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Services.HojasVida
{
    public class HojaVidaEquipoService
    {
        private const int CantidadMaximaEventos = 12;

        private readonly AppDbContext _context;
        private readonly HojaVidaPdfService _hojaVidaPdfService;
        private readonly IGoogleDriveService _googleDriveService;

        public HojaVidaEquipoService(
            AppDbContext context,
            HojaVidaPdfService hojaVidaPdfService,
            IGoogleDriveService googleDriveService)
        {
            _context = context;
            _hojaVidaPdfService = hojaVidaPdfService;
            _googleDriveService = googleDriveService;
        }

        public async Task<HojaVidaEquipo> GenerarOActualizarAsync(int equipoId)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Proveedor)
                .Include(e => e.Ubicacion)
                .Include(e => e.ResponsableInterno)
                .Include(e => e.CaracteristicasMetrologicas)
                .Include(e => e.ConfiguracionesControl)
                    .ThenInclude(c => c.TipoEventoMetrologico)
                .Include(e => e.HojaVida)
                .FirstOrDefaultAsync(e => e.EquipoId == equipoId && e.Activo);

            if (equipo == null)
                throw new InvalidOperationException("No se encontró el equipo o se encuentra inactivo.");

            var eventos = await CargarEventosParaHojaVidaAsync(equipoId);

            var pdfBytes = _hojaVidaPdfService.Generar(equipo, eventos);

            var nombreArchivo = $"HojaVida-{LimpiarNombreArchivo(equipo.Codigo)}.pdf";

            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                equipo.Codigo,
                "Documentos",
                "Hoja de Vida");

            if (equipo.HojaVida != null &&
                !string.IsNullOrWhiteSpace(equipo.HojaVida.GoogleDriveFileId))
            {
                await _googleDriveService.DeleteFileAsync(equipo.HojaVida.GoogleDriveFileId);
            }

            using var pdfStream = new MemoryStream(pdfBytes);

            var uploadResult = await _googleDriveService.UploadFileAsync(
                pdfStream,
                nombreArchivo,
                "application/pdf",
                folderId);

            if (equipo.HojaVida == null)
            {
                var nuevaHojaVida = new HojaVidaEquipo
                {
                    EquipoId = equipo.EquipoId,
                    NombreArchivoPdf = uploadResult.FileName,
                    GoogleDriveFileId = uploadResult.FileId,
                    RutaPdf = uploadResult.WebViewLink,
                    FechaUltimaGeneracion = DateTime.UtcNow.Date,
                    CantidadEventosIncluidos = eventos.Count,
                    Activa = true
                };

                _context.HojasVidaEquipo.Add(nuevaHojaVida);
                await _context.SaveChangesAsync();

                return nuevaHojaVida;
            }

            equipo.HojaVida.NombreArchivoPdf = uploadResult.FileName;
            equipo.HojaVida.GoogleDriveFileId = uploadResult.FileId;
            equipo.HojaVida.RutaPdf = uploadResult.WebViewLink;
            equipo.HojaVida.FechaUltimaGeneracion = DateTime.UtcNow.Date;
            equipo.HojaVida.CantidadEventosIncluidos = eventos.Count;
            equipo.HojaVida.Activa = true;

            await _context.SaveChangesAsync();

            return equipo.HojaVida;
        }

        private async Task<List<EventoMetrologico>> CargarEventosParaHojaVidaAsync(int equipoId)
        {
            var ultimosEventosIds = await _context.EventosMetrologicos
                .AsNoTracking()
                .Where(e => e.EquipoId == equipoId && e.Activo)
                .OrderByDescending(e => e.FechaEvento)
                .ThenByDescending(e => e.EventoMetrologicoId)
                .Take(CantidadMaximaEventos)
                .Select(e => e.EventoMetrologicoId)
                .ToListAsync();

            var eventos = await _context.EventosMetrologicos
                .AsNoTracking()
                .Where(e => ultimosEventosIds.Contains(e.EventoMetrologicoId))
                .Include(e => e.Equipo)
                    .ThenInclude(eq => eq.Proveedor)
                .Include(e => e.TipoEventoMetrologico)
                .Include(e => e.SubtipoEvento)
                .Include(e => e.ResponsableInterno)
                .Include(e => e.EventoCalibracionDato)
                    .ThenInclude(c => c.Laboratorio)
                .Include(e => e.EventoMantenimientoDato)
                    .ThenInclude(m => m.TipoMantenimiento)
                .Include(e => e.EventoVerificacionDato)
                .Include(e => e.ActividadesMantenimiento)
                .OrderBy(e => e.FechaEvento)
                .ThenBy(e => e.EventoMetrologicoId)
                .ToListAsync();

            return eventos;
        }

        private static string LimpiarNombreArchivo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "Equipo";

            var caracteresInvalidos = Path.GetInvalidFileNameChars();

            foreach (var caracter in caracteresInvalidos)
                texto = texto.Replace(caracter, '-');

            return texto.Trim();
        }
    }
}