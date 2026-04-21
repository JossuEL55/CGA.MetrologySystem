using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CGA.MetrologySystem.Controllers
{
    //Controlador para manejar la autenticación de usuarios, incluyendo el inicio de sesión
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
                return RedirectToAction("Index", "Home");
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
                return View(model);

            model.Correo = model.Correo.Trim().ToLower();

            var usuario = await _userManager.FindByEmailAsync(model.Correo);

            if (usuario == null || !usuario.Activo)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View(model);
            }

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

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}