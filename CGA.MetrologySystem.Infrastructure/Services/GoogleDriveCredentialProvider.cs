using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Infrastructure.Services
{
    public class GoogleDriveCredentialProvider : IGoogleDriveCredentialProvider
    {
        private readonly AppDbContext _context;

        public GoogleDriveCredentialProvider(AppDbContext context)
        {
            _context = context;
        }

        public string GetActiveRefreshToken()
        {
            var credential = _context.GoogleDriveCredentials
                .AsNoTracking()
                .FirstOrDefault(c => c.Activo);

            if (credential == null || string.IsNullOrWhiteSpace(credential.RefreshToken))
            {
                throw new Exception("No existe un refresh token activo en la base de datos. Primero autentica Google Drive desde /GoogleAuth/Login.");
            }

            return credential.RefreshToken;
        }
    }
}