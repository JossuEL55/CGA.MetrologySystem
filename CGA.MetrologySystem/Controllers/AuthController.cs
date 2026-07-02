using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<UsuarioSistema> _signInManager;
        private readonly UserManager<UsuarioSistema> _userManager;

        public AuthController(
            SignInManager<UsuarioSistema> signInManager,
            UserManager<UsuarioSistema> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirigirUsuarioAutenticadoPorRol();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Correo = model.Correo.Trim().ToLower();

            var usuario = await _userManager.FindByEmailAsync(model.Correo);

            if (usuario == null || !usuario.Activo)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View(model);
            }

            await NormalizarRolPrincipalAsync(usuario);

            var resultado = await _signInManager.PasswordSignInAsync(
                usuario.UserName!,
                model.Contrasena,
                model.Recordarme,
                lockoutOnFailure: false);

            if (!resultado.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View(model);
            }

            usuario.UltimoAcceso = DateTime.UtcNow;
            await _userManager.UpdateAsync(usuario);

            return await RedirigirPorRolAsync(usuario);
        }

        private async Task<IActionResult> RedirigirPorRolAsync(UsuarioSistema usuario)
        {
            var roles = await _userManager.GetRolesAsync(usuario);
            var rolPrincipal = ObtenerRolPrincipal(roles);

            if (rolPrincipal == RolesSistema.AdministradorSistema)
            {
                return RedirectToAction("Index", "DashboardMetrologico");
            }

            if (rolPrincipal == RolesSistema.AdministradorMetrologico ||
                rolPrincipal == RolesSistema.Tecnico)
            {
                return RedirectToAction("Index", "MaestroEquipos");
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task NormalizarRolPrincipalAsync(UsuarioSistema usuario)
        {
            var roles = await _userManager.GetRolesAsync(usuario);
            var rolPrincipal = ObtenerRolPrincipal(roles);

            if (string.IsNullOrWhiteSpace(rolPrincipal))
            {
                return;
            }

            var rolesARemover = roles
                .Where(r => RolesSistema.RolesBase.Contains(r) && r != rolPrincipal)
                .ToArray();

            if (rolesARemover.Any())
            {
                await _userManager.RemoveFromRolesAsync(usuario, rolesARemover);
            }

            if (!roles.Contains(rolPrincipal))
            {
                await _userManager.AddToRoleAsync(usuario, rolPrincipal);
            }
        }

        private static string? ObtenerRolPrincipal(IList<string> roles)
        {
            if (roles.Contains(RolesSistema.AdministradorSistema))
            {
                return RolesSistema.AdministradorSistema;
            }

            if (roles.Contains(RolesSistema.AdministradorMetrologico))
            {
                return RolesSistema.AdministradorMetrologico;
            }

            if (roles.Contains(RolesSistema.Tecnico))
            {
                return RolesSistema.Tecnico;
            }

            return null;
        }

        private IActionResult RedirigirUsuarioAutenticadoPorRol()
        {
            if (User.IsInRole(RolesSistema.AdministradorSistema))
            {
                return RedirectToAction("Index", "DashboardMetrologico");
            }

            if (User.IsInRole(RolesSistema.AdministradorMetrologico) ||
                User.IsInRole(RolesSistema.Tecnico))
            {
                return RedirectToAction("Index", "MaestroEquipos");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
