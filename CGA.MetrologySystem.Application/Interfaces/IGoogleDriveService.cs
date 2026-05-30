using CGA.MetrologySystem.Application.DTOs;

namespace CGA.MetrologySystem.Application.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<string?> FindFolderByNameAsync(string folderName, string parentFolderId);

        Task<string> CreateFolderAsync(string folderName, string parentFolderId);

        Task<string> GetOrCreateFolderAsync(string folderName, string parentFolderId);

        Task<GoogleDriveUploadResultDto> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string mimeType,
            string parentFolderId);

        Task<GoogleDriveFileContentDto> DownloadFileAsync(string fileId);

        string BuildViewUrl(string fileId);

        Task<string> EnsureEquiposRootFolderAsync();

        Task<string> EnsureEquipoFolderAsync(string codigoEquipo);

        Task<string> EnsureSubFolderAsync(string codigoEquipo, string subFolderName);

        Task DeleteFileAsync(string fileId);

        Task<string> EnsureNestedFolderAsync(
            string codigoEquipo,
            params string[] folderNames);
    }
}
