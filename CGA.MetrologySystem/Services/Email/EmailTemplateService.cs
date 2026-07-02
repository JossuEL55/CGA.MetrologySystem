using System.Net;
using CGA.MetrologySystem.Configuration;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public const string EmbeddedLogoToken = "cid:cga-logo";

        private readonly EmailBrandingSettings _settings;

        public EmailTemplateService(IOptions<EmailBrandingSettings> settings)
        {
            _settings = settings.Value;
        }

        public string ConstruirCorreo(EmailTemplateModel model)
        {
            var titulo = WebUtility.HtmlEncode(model.Titulo);
            var preheader = WebUtility.HtmlEncode(model.Preheader);
            var etiqueta = WebUtility.HtmlEncode(model.Etiqueta);
            var systemName = WebUtility.HtmlEncode(_settings.SystemName);
            var companyName = WebUtility.HtmlEncode(_settings.CompanyName);
            var logoSrc = WebUtility.HtmlEncode(ObtenerLogoSrc());
            var nivel = NormalizarNivel(model.Nivel);
            var colorPrincipal = ObtenerColorPrincipal(nivel);
            var colorSuave = ObtenerColorSuave(nivel);
            var textoNivel = WebUtility.HtmlEncode(ObtenerTextoNivel(nivel));
            var botonHtml = ConstruirBoton(model.TextoBoton, model.UrlBoton, colorPrincipal);
            var notaPie = string.IsNullOrWhiteSpace(model.NotaPie)
                ? "Este correo fue generado automáticamente por CGA Metrology System."
                : model.NotaPie;

            return $@"
<!doctype html>
<html lang=""es"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{titulo}</title>
</head>
<body style=""margin:0; padding:0; background:#f1f5f3; font-family:Arial, Helvetica, sans-serif; color:#1f2937;"">
    <div style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">
        {preheader}
    </div>

    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f1f5f3; margin:0; padding:28px 12px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:720px; background:#ffffff; border-radius:18px; overflow:hidden; border:1px solid #dce8e4; box-shadow:0 10px 28px rgba(15, 23, 42, 0.09);"">
                    <tr>
                        <td style=""background:linear-gradient(135deg, #2d5148 0%, #4f8584 100%); padding:26px 30px;"">
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td style=""vertical-align:middle;"">
                                        <img src=""{logoSrc}"" alt=""CGA"" width=""58"" height=""58"" style=""display:block; width:58px; height:58px; border-radius:13px; background:#ffffff; padding:6px;"">
                                    </td>
                                    <td style=""vertical-align:middle; padding-left:18px;"">
                                        <div style=""font-size:12px; font-weight:700; letter-spacing:0.08em; text-transform:uppercase; color:#cfe7df;"">{etiqueta}</div>
                                        <h1 style=""margin:6px 0 0; color:#ffffff; font-size:25px; line-height:1.2; font-weight:800;"">{titulo}</h1>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:28px 30px 8px;"">
                            <span style=""display:inline-block; background:{colorSuave}; color:{colorPrincipal}; border:1px solid {colorPrincipal}; border-radius:999px; padding:7px 13px; font-size:12px; font-weight:800; letter-spacing:0.04em; text-transform:uppercase;"">
                                {textoNivel}
                            </span>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:8px 30px 26px; font-size:15px; line-height:1.65; color:#334155;"">
                            {model.ContenidoHtml}
                            {botonHtml}
                        </td>
                    </tr>

                    <tr>
                        <td style=""background:#f8fafc; border-top:1px solid #e5e7eb; padding:20px 30px; color:#64748b; font-size:12px; line-height:1.55;"">
                            <strong style=""color:#24473d;"">{companyName}</strong><br>
                            {WebUtility.HtmlEncode(notaPie)}<br>
                            <span style=""color:#94a3b8;"">{systemName}</span>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public string ConstruirTablaDatos(IEnumerable<EmailTemplateRow> filas)
        {
            var rows = filas
                .Select(f => $@"
                    <tr>
                        <td style=""padding:12px 14px; background:#f8fafc; border:1px solid #e2e8f0; color:#24473d; font-weight:800; width:38%;"">{WebUtility.HtmlEncode(f.Etiqueta)}</td>
                        <td style=""padding:12px 14px; border:1px solid #e2e8f0; color:#1f2937;"">{WebUtility.HtmlEncode(f.Valor)}</td>
                    </tr>")
                .ToArray();

            return $@"
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse; margin:18px 0 20px;"">
                    {string.Join(string.Empty, rows)}
                </table>";
        }

        public string ConstruirLista(IEnumerable<string> items)
        {
            var lis = items
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => $@"<li style=""margin:0 0 8px;"">{WebUtility.HtmlEncode(i)}</li>")
                .ToArray();

            return $@"<ul style=""margin:12px 0 20px; padding-left:22px; color:#334155;"">{string.Join(string.Empty, lis)}</ul>";
        }

        private string ObtenerLogoSrc()
        {
            if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
            {
                return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{_settings.LogoPath.TrimStart('/')}";
            }

            var contentId = string.IsNullOrWhiteSpace(_settings.EmbeddedLogoContentId)
                ? "cga-logo"
                : _settings.EmbeddedLogoContentId.Trim();

            return $"cid:{contentId}";
        }

        private static string ConstruirBoton(string? textoBoton, string? urlBoton, string colorPrincipal)
        {
            if (string.IsNullOrWhiteSpace(textoBoton) || string.IsNullOrWhiteSpace(urlBoton))
            {
                return string.Empty;
            }

            var texto = WebUtility.HtmlEncode(textoBoton);
            var url = WebUtility.HtmlEncode(urlBoton);

            return $@"
                <div style=""margin-top:24px;"">
                    <a href=""{url}"" style=""display:inline-block; background:{colorPrincipal}; color:#ffffff; text-decoration:none; font-weight:800; border-radius:10px; padding:12px 18px;"">
                        {texto}
                    </a>
                </div>";
        }

        private static string NormalizarNivel(string? nivel)
        {
            return string.IsNullOrWhiteSpace(nivel)
                ? "info"
                : nivel.Trim().ToLowerInvariant();
        }

        private static string ObtenerColorPrincipal(string nivel)
        {
            return nivel switch
            {
                "critico" => "#b42318",
                "advertencia" => "#9a6a16",
                "exito" => "#3f7357",
                _ => "#43685d"
            };
        }

        private static string ObtenerColorSuave(string nivel)
        {
            return nivel switch
            {
                "critico" => "#fff1f1",
                "advertencia" => "#fff7e6",
                "exito" => "#eef7f1",
                _ => "#eef5f2"
            };
        }

        private static string ObtenerTextoNivel(string nivel)
        {
            return nivel switch
            {
                "critico" => "Crítico",
                "advertencia" => "Preventivo",
                "exito" => "Confirmación",
                _ => "Informativo"
            };
        }
    }
}
