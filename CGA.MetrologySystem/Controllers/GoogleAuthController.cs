using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Controllers
{
    public class GoogleAuthController : Controller
    {
        private readonly GoogleOAuthSettings _oauthSettings;
        private readonly AppDbContext _context;

        public GoogleAuthController(
            IOptions<GoogleOAuthSettings> oauthOptions,
            AppDbContext context)
        {
            _oauthSettings = oauthOptions.Value;
            _context = context;
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

                await SaveRefreshTokenToDatabaseAsync(token.RefreshToken);

                return Content("Autenticación completada correctamente. El refresh token fue guardado en la base de datos y Google Drive ya está listo para usarse.");
            }
            catch (Exception ex)
            {
                return Content($"Ocurrió un error al procesar la autorización OAuth: {ex.Message}");
            }
        }

        private async Task SaveRefreshTokenToDatabaseAsync(string refreshToken)
        {
            var credential = await _context.GoogleDriveCredentials
                .FirstOrDefaultAsync(c => c.Activo);

            if (credential == null)
            {
                credential = new GoogleDriveCredential
                {
                    RefreshToken = refreshToken,
                    FechaActualizacion = DateTime.UtcNow,
                    Activo = true
                };

                _context.GoogleDriveCredentials.Add(credential);
            }
            else
            {
                credential.RefreshToken = refreshToken;
                credential.FechaActualizacion = DateTime.UtcNow;
                credential.Activo = true;

                _context.GoogleDriveCredentials.Update(credential);
            }

            await _context.SaveChangesAsync();
        }
    }
}