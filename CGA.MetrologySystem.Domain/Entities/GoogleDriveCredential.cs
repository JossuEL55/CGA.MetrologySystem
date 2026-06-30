namespace CGA.MetrologySystem.Domain.Entities
{
    public class GoogleDriveCredential
    {
        public int GoogleDriveCredentialId { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;
    }
}