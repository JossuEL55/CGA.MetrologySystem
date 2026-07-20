using System.Net.Mail;
using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Notificaciones
{
    public class DestinatariosNotificacionService : IDestinatariosNotificacionService
    {
        private readonly UserManager<UsuarioSistema> _userManager;
        private readonly DestinatariosNotificacionesSettings _settings;

        public DestinatariosNotificacionService(
            UserManager<UsuarioSistema> userManager,
            IOptions<DestinatariosNotificacionesSettings> settings)
        {
            _userManager = userManager;
            _settings = settings.Value;
        }

        public bool EsModoPreproduccion => _settings.ModoPreproduccion;

        public bool PermiteCorreosRegistrados =>
            !_settings.ModoPreproduccion && _settings.PermitirCorreosRegistrados;

        public Task<List<string>> ObtenerAdministradoresSistemaAsync()
        {
            return ObtenerPorRolAsync(
                RolesSistema.AdministradorSistema,
                _settings.AdministradorSistema);
        }

        public Task<List<string>> ObtenerAdministradoresMetrologicosAsync()
        {
            return ObtenerPorRolAsync(
                RolesSistema.AdministradorMetrologico,
                _settings.AdministradorMetrologico);
        }

        public async Task<List<string>> ObtenerTodosAdministradoresAsync()
        {
            var administradores = await ObtenerAdministradoresSistemaAsync();
            administradores.AddRange(await ObtenerAdministradoresMetrologicosAsync());

            return administradores
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<string>> ObtenerPorRolAsync(
            string rol,
            string correoPreproduccion)
        {
            if (_settings.ModoPreproduccion)
            {
                return Normalizar(new[] { correoPreproduccion });
            }

            if (!_settings.PermitirCorreosRegistrados)
            {
                return new List<string>();
            }

            var usuarios = await _userManager.GetUsersInRoleAsync(rol);
            return Normalizar(
                usuarios
                    .Where(u => u.Activo)
                    .Select(u => u.Email));
        }

        private static List<string> Normalizar(IEnumerable<string?> correos)
        {
            return correos
                .Where(EsCorreoValido)
                .Select(c => c!.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool EsCorreoValido(string? correo)
        {
            return !string.IsNullOrWhiteSpace(correo) &&
                MailAddress.TryCreate(correo.Trim(), out _);
        }
    }
}
