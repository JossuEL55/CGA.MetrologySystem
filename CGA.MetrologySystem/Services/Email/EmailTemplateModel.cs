namespace CGA.MetrologySystem.Services.Email
{
    public class EmailTemplateModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string Preheader { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = "CGA Metrology System";
        public string Nivel { get; set; } = "info";
        public string ContenidoHtml { get; set; } = string.Empty;
        public string? TextoBoton { get; set; }
        public string? UrlBoton { get; set; }
        public string? NotaPie { get; set; }
    }

    public sealed record EmailTemplateRow(string Etiqueta, string Valor);
}
