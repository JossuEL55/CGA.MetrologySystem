namespace CGA.MetrologySystem.Services.Auditoria
{
    public interface IAuditoriaMetrologicaService
    {
        Task RegistrarAsync(AuditoriaMetrologicaRegistro registro);
    }
}
