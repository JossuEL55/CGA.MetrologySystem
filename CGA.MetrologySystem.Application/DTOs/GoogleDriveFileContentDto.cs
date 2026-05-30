namespace CGA.MetrologySystem.Application.DTOs
{
    public class GoogleDriveFileContentDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();

        public string FileName { get; set; } = string.Empty;

        public string MimeType { get; set; } = "application/octet-stream";
    }
}
