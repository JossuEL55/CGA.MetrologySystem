using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Application.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Options;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace CGA.MetrologySystem.Application.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GoogleDriveSettings _driveSettings;
        private readonly GoogleOAuthSettings _oauthSettings;
        private readonly IGoogleDriveCredentialProvider _credentialProvider;
        private readonly DriveService _driveService;

        public GoogleDriveService(
            IOptions<GoogleDriveSettings> driveOptions,
            IOptions<GoogleOAuthSettings> oauthOptions,
            IGoogleDriveCredentialProvider credentialProvider)
        {
            _driveSettings = driveOptions.Value;
            _oauthSettings = oauthOptions.Value;
            _credentialProvider = credentialProvider;
            _driveService = CreateDriveService();
        }

        private DriveService CreateDriveService()
        {
            var refreshToken = _credentialProvider.GetActiveRefreshToken();

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _oauthSettings.ClientId,
                        ClientSecret = _oauthSettings.ClientSecret
                    },
                    Scopes = new[] { DriveService.Scope.Drive }
                });

            var token = new TokenResponse
            {
                RefreshToken = refreshToken
            };

            var credential = new UserCredential(flow, "google-drive-user", token);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _oauthSettings.ApplicationName
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
            if (string.IsNullOrWhiteSpace(parentFolderId))
                throw new Exception("El parentFolderId está vacío o es inválido.");

            if (fileStream == null || !fileStream.CanRead)
                throw new Exception("El archivo no se pudo leer correctamente.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new Exception("El nombre del archivo es obligatorio.");

            if (string.IsNullOrWhiteSpace(mimeType))
                mimeType = "application/pdf";

            try
            {
                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                var fileMetadata = new DriveFile
                {
                    Name = fileName,
                    Parents = new List<string> { parentFolderId }
                };

                var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
                request.Fields = "id, name, webViewLink";

                var progress = await request.UploadAsync();

                if (progress.Status != UploadStatus.Completed)
                {
                    var detalle = progress.Exception?.Message ?? "Google Drive no devolvió detalle adicional.";
                    throw new Exception($"Subida incompleta. Estado: {progress.Status}. Detalle: {detalle}");
                }

                var uploadedFile = request.ResponseBody;

                if (uploadedFile == null || string.IsNullOrWhiteSpace(uploadedFile.Id))
                    throw new Exception("Google Drive no devolvió el archivo correctamente.");

                return new GoogleDriveUploadResultDto
                {
                    FileId = uploadedFile.Id,
                    FileName = uploadedFile.Name,
                    WebViewLink = uploadedFile.WebViewLink ?? BuildViewUrl(uploadedFile.Id)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al subir archivo a Google Drive: {ex.Message}", ex);
            }
        }

        public string BuildViewUrl(string fileId)
        {
            return $"https://drive.google.com/file/d/{fileId}/view";
        }

        public async Task<string> EnsureEquiposRootFolderAsync()
        {
            return await GetOrCreateFolderAsync("Equipos", _driveSettings.RootFolderId);
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