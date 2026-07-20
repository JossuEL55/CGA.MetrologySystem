using CGA.MetrologySystem.Controllers;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Models.Documentos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class DocumentosControllerTests
{
    [Theory]
    [InlineData("desconocido", 1)]
    [InlineData("ficha-tecnica", 999)]
    [InlineData("", 1)]
    public async Task VerPdf_DescriptorInvalido_RetornaNotFound(string tipo, int id)
    {
        await using var context = TestDbContextFactory.Create();
        var controller = CrearController(context, new TestGoogleDriveService());

        var result = await controller.VerPdf(tipo, id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData(" FICHA-TECNICA ", "Ficha tecnica")]
    [InlineData("hoja-vida", "Hoja de vida")]
    [InlineData("mantenimiento", "Mantenimiento")]
    [InlineData("verificacion", "Verificacion")]
    [InlineData("calibracion", "Certificado de calibracion")]
    public async Task VerPdf_TiposSoportados_RetornanModeloCompleto(string tipo, string titulo)
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, new TestGoogleDriveService());

        var result = await controller.VerPdf(tipo, 1, "/Equipos/Details/1");

        var model = Assert.IsType<PdfViewerViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(titulo, model.Titulo);
        Assert.Equal("EQ-DOC", model.CodigoEquipo);
        Assert.Equal("Equipo documental", model.NombreEquipo);
        Assert.StartsWith("/Current/ContenidoPdf", model.PdfUrl);
        Assert.Equal("/Equipos/Details/1", model.ReturnUrl);
    }

    [Fact]
    public async Task VerPdf_ReturnUrlExterna_UsaRetornoDelDescriptor()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, new TestGoogleDriveService());

        var result = await controller.VerPdf("calibracion", 1, "https://evil.example/phishing");

        var model = Assert.IsType<PdfViewerViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("/Calibraciones/Details", model.ReturnUrl);
    }

    [Fact]
    public async Task VerPdf_RefererDelMismoHost_UsaRutaComoRetorno()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, new TestGoogleDriveService());
        controller.Request.Headers.Referer = "https://metrologia.test/Calibraciones?pagina=2";

        var result = await controller.VerPdf("calibracion", 1);

        var model = Assert.IsType<PdfViewerViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("/Calibraciones?pagina=2", model.ReturnUrl);
    }

    [Fact]
    public async Task ContenidoPdf_DocumentoValido_DescargaComoPdfInlineYSanitizaNombre()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        context.EventosCalibracionDato.Local.Single().NombreArchivoCertificado = "certificado:final?.pdf";
        await context.SaveChangesAsync();
        var drive = new TestGoogleDriveService
        {
            DownloadResult = new()
            {
                Content = new byte[] { 10, 20, 30 },
                FileName = "drive.pdf",
                MimeType = "application/pdf"
            }
        };
        var controller = CrearController(context, drive);

        var result = await controller.ContenidoPdf("calibracion", 1);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(new byte[] { 10, 20, 30 }, file.FileContents);
        Assert.Equal("drive-calibracion", Assert.Single(drive.DownloadedFileIds));
        Assert.Equal("private, max-age=300", controller.Response.Headers.CacheControl);
        Assert.Contains(
            "certificado-final-.pdf",
            controller.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task ContenidoPdf_SinNombreConfigurado_UsaNombreDeDrive()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        context.EventosCalibracionDato.Local.Single().NombreArchivoCertificado = " ";
        await context.SaveChangesAsync();
        var drive = new TestGoogleDriveService
        {
            DownloadResult = new()
            {
                Content = new byte[] { 1 },
                FileName = "archivo-drive.pdf",
                MimeType = "application/pdf"
            }
        };
        var controller = CrearController(context, drive);

        await controller.ContenidoPdf("calibracion", 1);

        Assert.Contains("archivo-drive.pdf", controller.Response.Headers.ContentDisposition.ToString());
    }

    [Theory]
    [InlineData("evidencia-evento", "Evidencia visual")]
    [InlineData("evidencia-mantenimiento-item", "Evidencia de mantenimiento")]
    [InlineData("evidencia-verificacion-item", "Evidencia de verificacion")]
    public async Task VerImagen_TiposSoportados_RetornanModeloCompleto(string tipo, string titulo)
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        await context.SaveChangesAsync();
        var controller = CrearController(context, new TestGoogleDriveService());

        var result = await controller.VerImagen(tipo, 1, "/retorno-local");

        var model = Assert.IsType<ImageViewerViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(titulo, model.Titulo);
        Assert.Equal("EQ-DOC", model.CodigoEquipo);
        Assert.StartsWith("/Current/ContenidoImagen", model.ImagenUrl);
        Assert.Equal("/retorno-local", model.ReturnUrl);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("evidencia-evento")]
    public async Task VerImagen_DescriptorInvalido_RetornaNotFound(string tipo)
    {
        await using var context = TestDbContextFactory.Create();
        var controller = CrearController(context, new TestGoogleDriveService());

        var result = await controller.VerImagen(tipo, 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ContenidoImagen_ContentTypeDelDescriptorTienePrioridad()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        await context.SaveChangesAsync();
        var drive = new TestGoogleDriveService
        {
            DownloadResult = new()
            {
                Content = new byte[] { 5, 6 },
                FileName = "imagen-drive.png",
                MimeType = "image/webp"
            }
        };
        var controller = CrearController(context, drive);

        var result = await controller.ContenidoImagen("evidencia-evento", 1);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal("drive-evidencia", Assert.Single(drive.DownloadedFileIds));
    }

    [Fact]
    public async Task ContenidoImagen_ContentTypesInvalidos_UsaJpegComoFallback()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        context.EventosMantenimientoActividad.Local.Single().EvidenciaContentType = null;
        await context.SaveChangesAsync();
        var drive = new TestGoogleDriveService
        {
            DownloadResult = new()
            {
                Content = new byte[] { 7 },
                FileName = "archivo.bin",
                MimeType = "application/octet-stream"
            }
        };
        var controller = CrearController(context, drive);

        var result = await controller.ContenidoImagen("evidencia-mantenimiento-item", 1);

        Assert.Equal("image/jpeg", Assert.IsType<FileContentResult>(result).ContentType);
    }

    [Fact]
    public async Task VerPdf_DocumentoInactivoONoSubido_RetornaNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        SeedDocumentos(context);
        var ficha = context.FichasTecnicasEquipo.Local.Single();
        ficha.Activa = false;
        var hoja = context.HojasVidaEquipo.Local.Single();
        hoja.GoogleDriveFileId = null;
        await context.SaveChangesAsync();
        var controller = CrearController(context, new TestGoogleDriveService());

        Assert.IsType<NotFoundResult>(await controller.VerPdf("ficha-tecnica", 1));
        Assert.IsType<NotFoundResult>(await controller.VerPdf("hoja-vida", 1));
    }

    private static DocumentosController CrearController(
        AppDbContext context,
        TestGoogleDriveService drive)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("metrologia.test", 443);

        var controller = new DocumentosController(context, drive)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            Url = new TestUrlHelper()
        };
        return controller;
    }

    private static void SeedDocumentos(AppDbContext context)
    {
        var equipo = new Equipo
        {
            EquipoId = 1,
            Codigo = "EQ-DOC",
            Nombre = "Equipo documental",
            TipoEquipoId = 1,
            ProveedorId = 1,
            UbicacionId = 1,
            ResponsableInternoId = 1,
            Activo = true
        };
        var tipoEvento = new TipoEventoMetrologico
        {
            TipoEventoMetrologicoId = 1,
            Nombre = "Calibración"
        };
        var evento = new EventoMetrologico
        {
            EventoMetrologicoId = 1,
            EquipoId = 1,
            Equipo = equipo,
            TipoEventoMetrologicoId = 1,
            TipoEventoMetrologico = tipoEvento,
            SubtipoEventoId = 1,
            ResponsableInternoId = 1,
            FechaEvento = new DateTime(2026, 4, 15),
            Activo = true
        };
        var mantenimiento = new EventoMantenimientoDato
        {
            EventoMantenimientoDatoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            TipoMantenimientoId = 1,
            TipoMantenimiento = new TipoMantenimiento { TipoMantenimientoId = 1, Nombre = "Preventivo" },
            GoogleDriveFileId = "drive-mantenimiento",
            NombreArchivoPdf = "mantenimiento.pdf"
        };
        var verificacion = new EventoVerificacionDato
        {
            EventoVerificacionDatoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            GoogleDriveFileId = "drive-verificacion",
            NombreArchivoPdf = "verificacion.pdf"
        };
        var calibracion = new EventoCalibracionDato
        {
            EventoCalibracionDatoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            GoogleDriveFileId = "drive-calibracion",
            NombreArchivoCertificado = "calibracion.pdf"
        };
        var actividad = new EventoMantenimientoActividad
        {
            EventoMantenimientoActividadId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            DescripcionActividad = "Actividad",
            EvidenciaGoogleDriveFileId = "drive-actividad",
            EvidenciaNombreArchivo = "actividad.jpg",
            EvidenciaContentType = "image/jpeg",
            Orden = 1
        };
        var resultado = new EventoVerificacionResultado
        {
            EventoVerificacionResultadoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            DescripcionItem = "Resultado",
            EvidenciaGoogleDriveFileId = "drive-resultado",
            EvidenciaNombreArchivo = "resultado.webp",
            EvidenciaContentType = "image/webp",
            Orden = 1
        };

        context.TiposEquipo.Add(new TipoEquipo { TipoEquipoId = 1, Nombre = "Masa" });
        context.Equipos.Add(equipo);
        context.TiposEventoMetrologico.Add(tipoEvento);
        context.SubtiposEvento.Add(new SubtipoEvento { SubtipoEventoId = 1, Nombre = "Periódico", Activo = true });
        context.EventosMetrologicos.Add(evento);
        context.FichasTecnicasEquipo.Add(new FichaTecnicaEquipo
        {
            FichaTecnicaEquipoId = 1,
            EquipoId = 1,
            Equipo = equipo,
            NombreArchivoPdf = "ficha.pdf",
            GoogleDriveFileId = "drive-ficha",
            FechaUltimaGeneracion = new DateTime(2026, 4, 16),
            Activa = true
        });
        context.HojasVidaEquipo.Add(new HojaVidaEquipo
        {
            HojaVidaEquipoId = 1,
            EquipoId = 1,
            Equipo = equipo,
            NombreArchivoPdf = "hoja.pdf",
            GoogleDriveFileId = "drive-hoja",
            FechaUltimaGeneracion = new DateTime(2026, 4, 17),
            Activa = true
        });
        context.EventosMantenimientoDato.Add(mantenimiento);
        context.EventosVerificacionDato.Add(verificacion);
        context.EventosCalibracionDato.Add(calibracion);
        context.EventosMantenimientoActividad.Add(actividad);
        context.EventosVerificacionResultado.Add(resultado);
        context.EvidenciasEventoMetrologico.Add(new EvidenciaEventoMetrologico
        {
            EvidenciaEventoMetrologicoId = 1,
            EventoMetrologicoId = 1,
            EventoMetrologico = evento,
            NombreArchivo = "evidencia.png",
            ContentType = "image/png",
            GoogleDriveFileId = "drive-evidencia",
            RutaArchivo = "/evidencia.png",
            Activo = true
        });
    }
}
