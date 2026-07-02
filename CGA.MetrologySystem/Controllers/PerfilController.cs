using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Models.Perfil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    // Controlador para gestionar el perfil del usuario, incluyendo la visualización y actualización de información personal, así como el cambio de contraseña
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly UserManager<UsuarioSistema> _userManager;
        private readonly SignInManager<UsuarioSistema> _signInManager;

        public PerfilController(
            UserManager<UsuarioSistema> userManager,
            SignInManager<UsuarioSistema> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Perfil
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(usuario);

            var model = new PerfilViewModel
            {
                Correo = usuario.Email ?? string.Empty,
                NombreCompleto = usuario.NombreCompleto,
                Rol = ObtenerRolVisible(roles),
                Activo = usuario.Activo
            };

            return View(model);
        }

        // POST: /Perfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PerfilViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return NotFound();

            model.NombreCompleto = model.NombreCompleto.Trim();
            usuario.NombreCompleto = model.NombreCompleto;

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(usuario);

            model.Correo = usuario.Email ?? string.Empty;
            model.Activo = usuario.Activo;

            var roles = await _userManager.GetRolesAsync(usuario);
            model.Rol = ObtenerRolVisible(roles);

            TempData["MensajeExito"] = "Perfil actualizado correctamente.";
            return View(model);
        }

        //Métodos para cambiar la contraseña del usuario
        [HttpGet]
        public async Task<IActionResult> CambiarContrasena()
        {
            await CargarContextoCambioContrasenaAsync();
            return View(new CambiarContrasenaViewModel());
        }

        //Métdodo POST para cambiar la contraseña del usuario, validando la contraseña actual y asegurando que la nueva contraseña cumpla con los requisitos de seguridad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarContextoCambioContrasenaAsync();
                return View(model);
            }

            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return NotFound();

            var resultado = await _userManager.ChangePasswordAsync(
                usuario,
                model.ContrasenaActual,
                model.NuevaContrasena);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await CargarContextoCambioContrasenaAsync(usuario);
                return View(model);
            }

            usuario.DebeCambiarContrasena = false;
            var updateResult = await _userManager.UpdateAsync(usuario);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await CargarContextoCambioContrasenaAsync(usuario);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(usuario);

            TempData["MensajeExito"] = "Contraseña cambiada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarContextoCambioContrasenaAsync(UsuarioSistema? usuario = null)
        {
            usuario ??= await _userManager.GetUserAsync(User);
            ViewBag.RequiereCambioContrasena = usuario?.DebeCambiarContrasena == true;
        }

        private static string ObtenerRolVisible(IList<string> roles)
        {
            if (roles.Contains(RolesSistema.AdministradorSistema))
                return RolesSistema.ObtenerNombreVisible(RolesSistema.AdministradorSistema);

            if (roles.Contains(RolesSistema.AdministradorMetrologico))
                return RolesSistema.ObtenerNombreVisible(RolesSistema.AdministradorMetrologico);

            if (roles.Contains(RolesSistema.Tecnico))
                return RolesSistema.ObtenerNombreVisible(RolesSistema.Tecnico);

            return roles.FirstOrDefault() ?? "Sin rol";
        }
    }
}
