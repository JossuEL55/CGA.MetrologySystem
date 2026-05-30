namespace CGA.MetrologySystem.Models.Documentos
{
    public class ImageViewerViewModel
    {
        public string Tipo { get; set; } = string.Empty;

        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string NombreArchivo { get; set; } = string.Empty;

        public string EtiquetaDocumento { get; set; } = string.Empty;

        public string CodigoEquipo { get; set; } = string.Empty;

        public string NombreEquipo { get; set; } = string.Empty;

        public string ImagenUrl { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = "/";
    }
}
