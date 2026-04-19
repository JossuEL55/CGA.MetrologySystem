using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Application.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace CGA.MetrologySystem.Application.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GoogleDriveSettings _settings;
        private readonly DriveService _driveService;

        public GoogleDriveService(IOptions<GoogleDriveSettings> options)
        {
            _settings = options.Value;
            _driveService = CreateDriveService();
        }

        private DriveService CreateDriveService()
        {
            var serviceAccount = _settings.ServiceAccount;

            var credentialJson = JsonSerializer.Serialize(new
            {
                type = serviceAccount.Type,
                project_id = serviceAccount.ProjectId,
                private_key_id = serviceAccount.PrivateKeyId,
                private_key = serviceAccount.PrivateKey,
                client_email = serviceAccount.ClientEmail,
                client_id = serviceAccount.ClientId,
                auth_uri = serviceAccount.AuthUri,
                token_uri = serviceAccount.TokenUri,
                auth_provider_x509_cert_url = serviceAccount.AuthProviderX509CertUrl,
                client_x509_cert_url = serviceAccount.ClientX509CertUrl,
                universe_domain = serviceAccount.UniverseDomain
            });

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(credentialJson));

            var credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(DriveService.Scope.Drive);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CGA Metrology System"
            });
        }

        public async Task<string?> FindFolderByNameAsync(string folderName, string parentFolderId)
        {
            var request = _driveService.Files.List();
            request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName.Replace("'", "\\'")}' and '{parentFolderId}' in parents and trashed = false";
            request.Fields = "files(id, name)";
            request.PageSize = 10;

            var result = await request.ExecuteAsync();
            return result.Files.FirstOrDefault()?.Id;
        }

        public async Task<string> CreateFolderAsync(string folderName, string parentFolderId)
        {
            var fileMetadata = new DriveFile
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentFolderId }
            };

            var request = _driveService.Files.Create(fileMetadata);
            request.Fields = "id";

            var folder = await request.ExecuteAsync();
            return folder.Id;
        }

        public async Task<string> GetOrCreateFolderAsync(string folderName, string parentFolderId)
        {
            var existingFolderId = await FindFolderByNameAsync(folderName, parentFolderId);

            if (!string.IsNullOrWhiteSpace(existingFolderId))
                return existingFolderId;

            return await CreateFolderAsync(folderName, parentFolderId);
        }

        public async Task<GoogleDriveUploadResultDto> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string mimeType,
            string parentFolderId)
        {
            var fileMetadata = new DriveFile
            {
                Name = fileName,
                Parents = new List<string> { parentFolderId }
            };

            var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id, name, webViewLink";

            var progress = await request.UploadAsync();

            if (progress.Status != UploadStatus.Completed)
                throw new Exception($"No se pudo subir el archivo a Google Drive. Estado: {progress.Status}");

            var uploadedFile = request.ResponseBody;

            return new GoogleDriveUploadResultDto
            {
                FileId = uploadedFile.Id,
                FileName = uploadedFile.Name,
                WebViewLink = uploadedFile.WebViewLink ?? BuildViewUrl(uploadedFile.Id)
            };
        }

        public string BuildViewUrl(string fileId)
        {
            return $"https://drive.google.com/file/d/{fileId}/view";
        }

        public async Task<string> EnsureEquiposRootFolderAsync()
        {
            return await GetOrCreateFolderAsync("Equipos", _settings.RootFolderId);
        }

        public async Task<string> EnsureEquipoFolderAsync(string codigoEquipo)
        {
            var equiposRootFolderId = await EnsureEquiposRootFolderAsync();
            return await GetOrCreateFolderAsync(codigoEquipo, equiposRootFolderId);
        }

        public async Task<string> EnsureSubFolderAsync(string codigoEquipo, string subFolderName)
        {
            var equipoFolderId = await EnsureEquipoFolderAsync(codigoEquipo);
            return await GetOrCreateFolderAsync(subFolderName, equipoFolderId);
        }
    }
}