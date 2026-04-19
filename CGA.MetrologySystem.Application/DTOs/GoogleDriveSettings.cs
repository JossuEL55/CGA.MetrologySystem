
namespace CGA.MetrologySystem.Application.DTOs
{
    public class GoogleDriveSettings
    {
        public string RootFolderId { get; set; } = string.Empty;
        public GoogleServiceAccountSettings ServiceAccount { get; set; } = new();
    }
}