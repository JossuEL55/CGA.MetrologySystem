using CGA.MetrologySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CGA.MetrologySystem.Infrastructure.Persistence
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<UsuarioSistema>>();

            string[] roles = { "Administrador", "Tecnico" };

            foreach (var role in roles)
            {
                var existe = await roleManager.RoleExistsAsync(role);
                if (!existe)
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await CrearOActualizarUsuarioAsync(
                userManager,
                email: "gerencia@cga.com.ec",
                nombreCompleto: "Administrador General",
                password: "Admin1234",
                rol: "Administrador");

            await CrearOActualizarUsuarioAsync(
                userManager,
                email: "tecnico@cga.com.ec",
                nombreCompleto: "Técnico CGA",
                password: "Tecnico1234",
                rol: "Tecnico");
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
                    Activo = true
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

            if (!await userManager.IsInRoleAsync(usuario, rol))
            {
                var roleResult = await userManager.AddToRoleAsync(usuario, rol);
                if (!roleResult.Succeeded)
                {
                    var errores = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"No se pudo asignar el rol {rol} al usuario {email}: {errores}");
                }
            }
        }
    }
}