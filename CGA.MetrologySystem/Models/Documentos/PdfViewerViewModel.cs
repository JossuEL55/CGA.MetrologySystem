namespace CGA.MetrologySystem.Models.Documentos
{
    public class PdfViewerViewModel
    {
        public string Tipo { get; set; } = string.Empty;

        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string NombreArchivo { get; set; } = string.Empty;

        public string EtiquetaDocumento { get; set; } = string.Empty;

        public string? CodigoEquipo { get; set; }

        public string? NombreEquipo { get; set; }

        public string PdfUrl { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = "/";
    }
}
