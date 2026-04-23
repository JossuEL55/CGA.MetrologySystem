using CGA.MetrologySystem.Application.DTOs;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Controllers
{
    public class GoogleAuthController : Controller
    {
        private readonly GoogleOAuthSettings _oauthSettings;
        private readonly GoogleOAuthTokenStorageSettings _tokenStorageSettings;

        public GoogleAuthController(
            IOptions<GoogleOAuthSettings> oauthOptions,
            IOptions<GoogleOAuthTokenStorageSettings> tokenStorageOptions)
        {
            _oauthSettings = oauthOptions.Value;
            _tokenStorageSettings = tokenStorageOptions.Value;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var authorizationUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(_oauthSettings.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(_oauthSettings.RedirectUri)}" +
                "&response_type=code" +
                $"&scope={Uri.EscapeDataString(DriveService.Scope.Drive)}" +
                "&access_type=offline" +
                "&prompt=consent";

            return Redirect(authorizationUrl);
        }

        [HttpGet("/oauth2callback")]
        public async Task<IActionResult> OAuth2Callback(string code, string? error = null)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                return Content($"Google devolvió un error durante la autorización: {error}");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return Content("No se recibió el código de autorización de Google.");
            }

            try
            {
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

                var token = await flow.ExchangeCodeForTokenAsync(
                    userId: "google-drive-user",
                    code: code,
                    redirectUri: _oauthSettings.RedirectUri,
                    taskCancellationToken: CancellationToken.None);

                if (string.IsNullOrWhiteSpace(token.RefreshToken))
                {
                    return Content("Google no devolvió refresh_token. Asegúrate de usar access_type=offline y prompt=consent, y de autorizar con la cuenta correcta.");
                }

                SaveRefreshTokenToFile(token.RefreshToken);

                return Content("Autenticación completada correctamente. El refresh token fue guardado y Google Drive ya está listo para usarse.");
            }
            catch (Exception ex)
            {
                return Content($"Ocurrió un error al procesar la autorización OAuth: {ex.Message}");
            }
        }

        private void SaveRefreshTokenToFile(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(_tokenStorageSettings.RefreshTokenFilePath))
                throw new Exception("No se ha configurado la ruta del archivo para guardar el refresh token.");

            var fullPath = Path.GetFullPath(_tokenStorageSettings.RefreshTokenFilePath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new Exception("No se pudo determinar el directorio del archivo de refresh token.");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            System.IO.File.WriteAllText(fullPath, refreshToken);
        }
    }
}