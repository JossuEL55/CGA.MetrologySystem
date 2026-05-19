using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Pdf;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Services.FichasTecnicas
{
    public class FichaTecnicaEquipoService
    {
        private const int CantidadMaximaEventos = 10;

        private readonly AppDbContext _context;
        private readonly FichaTecnicaPdfService _fichaTecnicaPdfService;
        private readonly IGoogleDriveService _googleDriveService;

        public FichaTecnicaEquipoService(
            AppDbContext context,
            FichaTecnicaPdfService fichaTecnicaPdfService,
            IGoogleDriveService googleDriveService)
        {
            _context = context;
            _fichaTecnicaPdfService = fichaTecnicaPdfService;
            _googleDriveService = googleDriveService;
        }

        public async Task<FichaTecnicaEquipo> GenerarOActualizarAsync(int equipoId)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Proveedor)
                .Include(e => e.Ubicacion)
                .Include(e => e.ResponsableInterno)
                .Include(e => e.CaracteristicasMetrologicas)
                .Include(e => e.FichaTecnica)
                .FirstOrDefaultAsync(e => e.EquipoId == equipoId && e.Activo);

            if (equipo == null)
                throw new InvalidOperationException("No se encontró el equipo o se encuentra inactivo.");

            var eventos = await CargarEventosParaFichaAsync(equipoId);

            var pdfBytes = _fichaTecnicaPdfService.Generar(equipo, eventos);

            var nombreArchivo = $"FichaTecnica-{LimpiarNombreArchivo(equipo.Codigo)}.pdf";

            var folderId = await _googleDriveService.EnsureNestedFolderAsync(
                equipo.Codigo,
                "Documentos",
                "Ficha Técnica");

            if (equipo.FichaTecnica != null &&
                !string.IsNullOrWhiteSpace(equipo.FichaTecnica.GoogleDriveFileId))
            {
                await _googleDriveService.DeleteFileAsync(equipo.FichaTecnica.GoogleDriveFileId);
            }

            using var pdfStream = new MemoryStream(pdfBytes);

            var uploadResult = await _googleDriveService.UploadFileAsync(
                pdfStream,
                nombreArchivo,
                "application/pdf",
                folderId);

            if (equipo.FichaTecnica == null)
            {
                var nuevaFicha = new FichaTecnicaEquipo
                {
                    EquipoId = equipo.EquipoId,
                    NombreArchivoPdf = uploadResult.FileName,
                    GoogleDriveFileId = uploadResult.FileId,
                    RutaPdf = uploadResult.WebViewLink,
                    FechaUltimaGeneracion = DateTime.UtcNow.Date,
                    CantidadEventosIncluidos = eventos.Count,
                    Activa = true
                };

                _context.FichasTecnicasEquipo.Add(nuevaFicha);
                await _context.SaveChangesAsync();

                return nuevaFicha;
            }

            equipo.FichaTecnica.NombreArchivoPdf = uploadResult.FileName;
            equipo.FichaTecnica.GoogleDriveFileId = uploadResult.FileId;
            equipo.FichaTecnica.RutaPdf = uploadResult.WebViewLink;
            equipo.FichaTecnica.FechaUltimaGeneracion = DateTime.UtcNow.Date;
            equipo.FichaTecnica.CantidadEventosIncluidos = eventos.Count;
            equipo.FichaTecnica.Activa = true;

            await _context.SaveChangesAsync();

            return equipo.FichaTecnica;
        }

        private async Task<List<EventoMetrologico>> CargarEventosParaFichaAsync(int equipoId)
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