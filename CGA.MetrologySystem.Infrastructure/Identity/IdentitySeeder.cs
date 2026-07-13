using CGA.MetrologySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CGA.MetrologySystem.Infrastructure.Persistence
{
    public static class IdentitySeeder
    {
        private const string AdministradorLegacy = "Administrador";

        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<UsuarioSistema>>();

            foreach (var role in RolesSistema.RolesBase)
            {
                var existe = await roleManager.RoleExistsAsync(role);
                if (!existe)
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }  

            await RemoverRolAdministradorLegacyAsync(roleManager, userManager);

            await CrearOActualizarUsuarioAsync(
                userManager,
                email: "gerencia@cga.com.ec",
                nombreCompleto: "Administrador General",
                password: "Admin1234",
                rol: RolesSistema.AdministradorSistema);

            await CrearOActualizarUsuarioAsync(
                userManager,
                email: "admin.metrologico@cga.com.ec",
                nombreCompleto: "Administrador Metrologico",
                password: "Metrologico1234",
                rol: RolesSistema.AdministradorMetrologico);

            await CrearOActualizarUsuarioAsync(
                userManager,
                email: "tecnico@cga.com.ec",
                nombreCompleto: "Técnico CGA",
                password: "Tecnico1234",
                rol: RolesSistema.Tecnico);
        }

        private static async Task CrearOActualizarUsuarioAsync(
            UserManager<UsuarioSistema> userManager,
            string email,
            string nombreCompleto,
            string password,
            string rol)
        {
            var usuario = await userManager.FindByEmailAsync(email);

            if (usuario == null)
            {
                usuario = new UsuarioSistema
                {
                    UserName = email,
                    Email = email,
                    NombreCompleto = nombreCompleto,
                    EmailConfirmed = true,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var resultado = await userManager.CreateAsync(usuario, password);

                if (!resultado.Succeeded)
                {
                    var errores = string.Join(" | ", resultado.Errors.Select(e => e.Description));
                    throw new Exception($"No se pudo crear el usuario {email}: {errores}");
                }
            }
            else
            {
                var huboCambios = false;

                if (usuario.NombreCompleto != nombreCompleto)
                {
                    usuario.NombreCompleto = nombreCompleto;
                    huboCambios = true;
                }

                if (!usuario.Activo)
                {
                    usuario.Activo = true;
                    huboCambios = true;
                }

                if (!usuario.EmailConfirmed)
                {
                    usuario.EmailConfirmed = true;
                    huboCambios = true;
                }

                if (usuario.FechaCreacion == default)
                {
                    usuario.FechaCreacion = DateTime.UtcNow;
                    huboCambios = true;
                }

                if (huboCambios)
                {
                    var updateResult = await userManager.UpdateAsync(usuario);
                    if (!updateResult.Succeeded)
                    {
                        var errores = string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                        throw new Exception($"No se pudo actualizar el usuario {email}: {errores}");
                    }
                }
            }

            await SincronizarRolUnicoAsync(userManager, usuario, rol);
        }

        private static async Task SincronizarRolUnicoAsync(
            UserManager<UsuarioSistema> userManager,
            UsuarioSistema usuario,
            string rolPermitido)
        {
            var rolesActuales = await userManager.GetRolesAsync(usuario);
            var rolesARemover = rolesActuales
                .Where(r => RolesSistema.RolesBase.Contains(r) && r != rolPermitido)
                .ToArray();

            if (rolesARemover.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(usuario, rolesARemover);
                if (!removeResult.Succeeded)
                {
                    var errores = string.Join(" | ", removeResult.Errors.Select(e => e.Description));
                    throw new Exception($"No se pudo limpiar roles del usuario {usuario.Email}: {errores}");
                }
            }

            if (!await userManager.IsInRoleAsync(usuario, rolPermitido))
            {
                var roleResult = await userManager.AddToRoleAsync(usuario, rolPermitido);
                if (!roleResult.Succeeded)
                {
                    var errores = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"No se pudo asignar el rol {rolPermitido} al usuario {usuario.Email}: {errores}");
                }
            }
        }

        private static async Task RemoverRolAdministradorLegacyAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<UsuarioSistema> userManager)
        {
            var rolLegacy = await roleManager.FindByNameAsync(AdministradorLegacy);

            if (rolLegacy == null)
            {
                return;
            }

            var usuariosLegacy = await userManager.GetUsersInRoleAsync(AdministradorLegacy);

            foreach (var usuario in usuariosLegacy)
            {
                await AsegurarRolAsync(userManager, usuario, RolesSistema.AdministradorSistema);
                await AsegurarRolAsync(userManager, usuario, RolesSistema.AdministradorMetrologico);

                var removeResult = await userManager.RemoveFromRoleAsync(usuario, AdministradorLegacy);
                if (!removeResult.Succeeded)
                {
                    var errores = string.Join(" | ", removeResult.Errors.Select(e => e.Description));
                    throw new Exception($"No se pudo remover el rol legacy del usuario {usuario.Email}: {errores}");
                }
            }

            var usuariosRestantes = await userManager.GetUsersInRoleAsync(AdministradorLegacy);
            if (!usuariosRestantes.Any())
            {
                var deleteResult = await roleManager.DeleteAsync(rolLegacy);
                if (!deleteResult.Succeeded)
                {
                    var errores = string.Join(" | ", deleteResult.Errors.Select(e => e.Description));
                    throw new Exception($"No se pudo eliminar el rol legacy {AdministradorLegacy}: {errores}");
                }
            }
        }

        private static async Task AsegurarRolAsync(
            UserManager<UsuarioSistema> userManager,
            UsuarioSistema usuario,
            string rol)
        {
            if (await userManager.IsInRoleAsync(usuario, rol))
            {
                return;
            }

            var resultado = await userManager.AddToRoleAsync(usuario, rol);

            if (!resultado.Succeeded)
            {
                var errores = string.Join(" | ", resultado.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo asignar el rol {rol} al usuario {usuario.Email}: {errores}");
            }
        }
    }
}
