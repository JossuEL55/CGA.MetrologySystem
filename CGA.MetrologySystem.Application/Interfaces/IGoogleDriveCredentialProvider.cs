namespace CGA.MetrologySystem.Application.Interfaces
{
    public interface IGoogleDriveCredentialProvider
    {
        string GetActiveRefreshToken();
    }
}