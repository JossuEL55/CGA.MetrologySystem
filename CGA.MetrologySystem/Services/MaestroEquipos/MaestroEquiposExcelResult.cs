namespace CGA.MetrologySystem.Services.MaestroEquipos
{
    public class MaestroEquiposExcelResult
    {
        public byte[] Contenido { get; set; } = Array.Empty<byte>();

        public string NombreArchivo { get; set; } = string.Empty;

        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
