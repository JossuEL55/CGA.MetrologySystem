using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Services.Email;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace CGA.MetrologySystem.Tests;

public class EmailTemplateServiceTests
{
    [Fact]
    public void ConstruirCorreo_SinUrlPublica_UsaLogoEmbebidoPredeterminado()
    {
        var service = CrearServicio();

        var html = service.ConstruirCorreo(NuevoModelo());

        Assert.Contains("src=\"cid:cga-logo\"", html);
        Assert.Contains("CGA Metrology System", html);
        Assert.Contains("CGA-OIL Inspection Services", html);
    }

    [Fact]
    public void ConstruirCorreo_ConUrlPublica_CombinaBaseYRutaSinDuplicarBarras()
    {
        var service = CrearServicio(new EmailBrandingSettings
        {
            PublicBaseUrl = "https://metrologia.example.com/",
            LogoPath = "/assets/logo.png"
        });

        var html = service.ConstruirCorreo(NuevoModelo());

        Assert.Contains("src=\"https://metrologia.example.com/assets/logo.png\"", html);
    }

    [Fact]
    public void ConstruirCorreo_ConContentIdPersonalizado_EliminaEspacios()
    {
        var service = CrearServicio(new EmailBrandingSettings
        {
            EmbeddedLogoContentId = "  logo-personalizado  "
        });

        var html = service.ConstruirCorreo(NuevoModelo());

        Assert.Contains("src=\"cid:logo-personalizado\"", html);
    }

    [Theory]
    [InlineData("critico", "Crítico", "#b42318")]
    [InlineData(" ADVERTENCIA ", "Preventivo", "#9a6a16")]
    [InlineData("exito", "Confirmación", "#3f7357")]
    [InlineData(null, "Informativo", "#43685d")]
    [InlineData("desconocido", "Informativo", "#43685d")]
    public void ConstruirCorreo_NivelConfiguraTextoYColor(
        string? nivel,
        string textoEsperado,
        string colorEsperado)
    {
        var service = CrearServicio();
        var model = NuevoModelo();
        model.Nivel = nivel!;

        var html = service.ConstruirCorreo(model);

        Assert.Contains(textoEsperado, WebUtility.HtmlDecode(html));
        Assert.Contains(colorEsperado, html);
    }

    [Fact]
    public void ConstruirCorreo_ContenidoDeTextoSeCodificaPeroContenidoHtmlSeConserva()
    {
        var service = CrearServicio(new EmailBrandingSettings
        {
            SystemName = "Sistema <CGA>",
            CompanyName = "Compañía & Asociados"
        });
        var model = new EmailTemplateModel
        {
            Titulo = "Alerta <urgente>",
            Preheader = "Equipo & control",
            Etiqueta = "Control > aviso",
            ContenidoHtml = "<strong>Contenido permitido</strong>"
        };

        var html = service.ConstruirCorreo(model);

        Assert.Contains("Alerta &lt;urgente&gt;", html);
        Assert.Contains("Equipo &amp; control", html);
        Assert.Contains("Control &gt; aviso", html);
        Assert.Contains("Sistema &lt;CGA&gt;", html);
        Assert.Contains("Compañía & Asociados", WebUtility.HtmlDecode(html));
        Assert.Contains("<strong>Contenido permitido</strong>", html);
    }

    [Fact]
    public void ConstruirCorreo_BotonCompletoCodificaTextoYUrl()
    {
        var service = CrearServicio();
        var model = NuevoModelo();
        model.TextoBoton = "Abrir <equipo>";
        model.UrlBoton = "https://example.com/ver?a=1&b=2";

        var html = service.ConstruirCorreo(model);

        Assert.Contains("Abrir &lt;equipo&gt;", html);
        Assert.Contains("https://example.com/ver?a=1&amp;b=2", html);
        Assert.Contains("<a href=", html);
    }

    [Theory]
    [InlineData(null, "https://example.com")]
    [InlineData("Abrir", null)]
    [InlineData("  ", "https://example.com")]
    public void ConstruirCorreo_BotonIncompleto_NoGeneraEnlace(string? texto, string? url)
    {
        var service = CrearServicio();
        var model = NuevoModelo();
        model.TextoBoton = texto;
        model.UrlBoton = url;

        var html = service.ConstruirCorreo(model);

        Assert.DoesNotContain("<a href=", html);
    }

    [Fact]
    public void ConstruirCorreo_NotaPersonalizadaSeCodifica()
    {
        var service = CrearServicio();
        var model = NuevoModelo();
        model.NotaPie = "Aviso <interno> & automático";

        var html = service.ConstruirCorreo(model);

        Assert.Contains("Aviso <interno> & automático", WebUtility.HtmlDecode(html));
        Assert.Contains("&lt;interno&gt;", html);
        Assert.DoesNotContain("Este correo fue generado automáticamente", html);
    }

    [Fact]
    public void ConstruirTablaDatos_GeneraFilasYCodificaValores()
    {
        var service = CrearServicio();

        var html = service.ConstruirTablaDatos(new[]
        {
            new EmailTemplateRow("Equipo", "Balanza <01>"),
            new EmailTemplateRow("Estado & prioridad", "Crítico")
        });

        Assert.Equal(2, ContarOcurrencias(html, "<tr>"));
        Assert.Contains("Balanza &lt;01&gt;", html);
        Assert.Contains("Estado &amp; prioridad", html);
    }

    [Fact]
    public void ConstruirLista_OmiteElementosVaciosYCodificaContenido()
    {
        var service = CrearServicio();

        var html = service.ConstruirLista(new[] { "Primero", " ", "Segundo <control>", string.Empty });

        Assert.Equal(2, ContarOcurrencias(html, "<li style="));
        Assert.Contains("Segundo &lt;control&gt;", html);
    }

    private static EmailTemplateService CrearServicio(EmailBrandingSettings? settings = null)
    {
        return new EmailTemplateService(Options.Create(settings ?? new EmailBrandingSettings()));
    }

    private static EmailTemplateModel NuevoModelo()
    {
        return new EmailTemplateModel
        {
            Titulo = "Alerta metrológica",
            Preheader = "Existe un control pendiente",
            Etiqueta = "Metrología",
            Nivel = "info",
            ContenidoHtml = "<p>Contenido</p>"
        };
    }

    private static int ContarOcurrencias(string texto, string valor)
    {
        return texto.Split(valor, StringSplitOptions.None).Length - 1;
    }
}
