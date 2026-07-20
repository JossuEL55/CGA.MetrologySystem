using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Application.DTOs.Rules;
using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Services.Auditoria;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Notificaciones;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Tests;

internal sealed class RecordingEmailService : IEmailService
{
    public List<RecordedEmail> Emails { get; } = new();

    public Exception? ExceptionToThrow { get; set; }

    public Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        return EnviarCorreoAsync(new[] { destinatario }, asunto, cuerpoHtml);
    }

    public Task EnviarCorreoAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string cuerpoHtml)
    {
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        Emails.Add(new RecordedEmail(destinatarios.ToList(), asunto, cuerpoHtml));
        return Task.CompletedTask;
    }
}

internal sealed record RecordedEmail(
    IReadOnlyList<string> Recipients,
    string Subject,
    string HtmlBody);

internal sealed class TestUserManager : UserManager<UsuarioSistema>
{
    private readonly Dictionary<string, List<UsuarioSistema>> _usersByRole;

    public TestUserManager(Dictionary<string, List<UsuarioSistema>>? usersByRole = null)
        : base(
            new TestUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<UsuarioSistema>(),
            Array.Empty<IUserValidator<UsuarioSistema>>(),
            Array.Empty<IPasswordValidator<UsuarioSistema>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<UsuarioSistema>>.Instance)
    {
        _usersByRole = usersByRole ?? new Dictionary<string, List<UsuarioSistema>>();
    }

    public override Task<IList<UsuarioSistema>> GetUsersInRoleAsync(string roleName)
    {
        IList<UsuarioSistema> users = _usersByRole.TryGetValue(roleName, out var configured)
            ? configured
            : new List<UsuarioSistema>();

        return Task.FromResult(users);
    }
}

internal sealed class TestUserStore : IUserStore<UsuarioSistema>
{
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(UsuarioSistema user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(UsuarioSistema user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(
        UsuarioSistema user,
        string? userName,
        CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(
        UsuarioSistema user,
        CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(
        UsuarioSistema user,
        string? normalizedName,
        CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(UsuarioSistema user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> UpdateAsync(UsuarioSistema user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(UsuarioSistema user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<UsuarioSistema?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<UsuarioSistema?>(null);

    public Task<UsuarioSistema?> FindByNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken) => Task.FromResult<UsuarioSistema?>(null);
}

internal sealed class TestGoogleDriveService : IGoogleDriveService
{
    public GoogleDriveFileContentDto DownloadResult { get; set; } = new()
    {
        Content = new byte[] { 1, 2, 3 },
        FileName = "archivo.bin",
        MimeType = "application/octet-stream"
    };

    public List<string> DownloadedFileIds { get; } = new();

    public Task<GoogleDriveFileContentDto> DownloadFileAsync(string fileId)
    {
        DownloadedFileIds.Add(fileId);
        return Task.FromResult(DownloadResult);
    }

    public Task<string?> FindFolderByNameAsync(string folderName, string parentFolderId) =>
        Task.FromResult<string?>(null);

    public Task<string> CreateFolderAsync(string folderName, string parentFolderId) =>
        Task.FromResult($"folder-{folderName}");

    public Task<string> GetOrCreateFolderAsync(string folderName, string parentFolderId) =>
        Task.FromResult($"folder-{folderName}");

    public Task<GoogleDriveUploadResultDto> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string mimeType,
        string parentFolderId) => Task.FromResult(new GoogleDriveUploadResultDto());

    public string BuildViewUrl(string fileId) => $"https://drive.test/{fileId}";

    public Task<string> EnsureEquiposRootFolderAsync() => Task.FromResult("equipos");

    public Task<string> EnsureEquipoFolderAsync(string codigoEquipo) =>
        Task.FromResult($"equipo-{codigoEquipo}");

    public Task<string> EnsureSubFolderAsync(string codigoEquipo, string subFolderName) =>
        Task.FromResult($"{codigoEquipo}-{subFolderName}");

    public Task DeleteFileAsync(string fileId) => Task.CompletedTask;

    public Task<string> EnsureNestedFolderAsync(string codigoEquipo, params string[] folderNames) =>
        Task.FromResult(string.Join("/", new[] { codigoEquipo }.Concat(folderNames)));
}

internal sealed class TestUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; } = new();

    public string? Action(UrlActionContext actionContext)
    {
        var controller = string.IsNullOrWhiteSpace(actionContext.Controller)
            ? "Current"
            : actionContext.Controller;
        return $"/{controller}/{actionContext.Action}";
    }

    public string? Content(string? contentPath) => contentPath;

    public bool IsLocalUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && url.StartsWith('/') && !url.StartsWith("//");

    public string? Link(string? routeName, object? values) => $"/route/{routeName}";

    public string? RouteUrl(UrlRouteContext routeContext) => $"/route/{routeContext.RouteName}";
}

internal sealed class TestMetrologyRulesService : IMetrologyRulesService
{
    public ResultadoReglaMetrologicaDto EvaluationResult { get; set; } = new();

    public DateTime? NextDate { get; set; }

    public Task<ResultadoReglaMetrologicaDto> EvaluarEventoAsync(
        int equipoId,
        int tipoEventoMetrologicoId,
        DateTime fechaEvento,
        int subtipoEventoId,
        string? justificacionExtraordinario,
        bool esHistorico = false) => Task.FromResult(EvaluationResult);

    public Task<DateTime?> CalcularProximaFechaAsync(
        int equipoId,
        int tipoEventoMetrologicoId,
        DateTime fechaEvento) => Task.FromResult(NextDate);
}

internal sealed class TestNotificacionMetrologicaService : INotificacionMetrologicaService
{
    public Task NotificarEventoExtraordinarioAsync(int eventoMetrologicoId) => Task.CompletedTask;

    public Task<ResultadoReintentoNotificacion> ReintentarNotificacionFallidaAsync(int notificacionEnviadaId) =>
        Task.FromResult(new ResultadoReintentoNotificacion
        {
            FueExitosa = true,
            Mensaje = string.Empty
        });

    public Task NotificarReemplazoCertificadoCalibracionAsync(
        int eventoCalibracionDatoId,
        string? nombreCertificadoAnterior,
        string? nombreCertificadoNuevo,
        string? usuarioResponsable) => Task.CompletedTask;

    public Task NotificarEdicionCriticaVerificacionAsync(
        int eventoVerificacionDatoId,
        string? usuarioResponsable,
        IReadOnlyCollection<string> cambiosCriticos) => Task.CompletedTask;

    public Task NotificarEdicionCriticaMantenimientoAsync(
        int eventoMantenimientoDatoId,
        string? usuarioResponsable,
        IReadOnlyCollection<string> cambiosCriticos) => Task.CompletedTask;
}

internal sealed class TestAuditoriaMetrologicaService : IAuditoriaMetrologicaService
{
    public List<AuditoriaMetrologicaRegistro> Records { get; } = new();

    public Task RegistrarAsync(AuditoriaMetrologicaRegistro registro)
    {
        Records.Add(registro);
        return Task.CompletedTask;
    }
}
