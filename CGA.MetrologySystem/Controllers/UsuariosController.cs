using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Models.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Domain.Entities;

namespace CGA.MetrologySystem.Controllers
{
    // Controlador para la gestión de usuarios, accesible solo para administradores
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<UsuarioSistema> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        //Constructor 
        public UsuariosController(
               UserManager<UsuarioSistema> userManager,
               RoleManager<IdentityRole> roleManager,
               AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // Método para listar todos los usuarios con su información básica y rol asignado
        public async Task<IActionResult> Index()
        {
            var usuarios = _userManager.Users.ToList();
            var modelo = new List<UsuarioListadoViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);

                modelo.Add(new UsuarioListadoViewModel
                {
                    Id = usuario.Id,
                    Correo = usuario.Email ?? string.Empty,
                    NombreCompleto = usuario.NombreCompleto,
                    Activo = usuario.Activo,
                    Rol = roles.FirstOrDefault() ?? "Sin rol"
                });
            }

            return View(modelo);
        }

        // Método para mostrar el formulario de creación de un nuevo usuario, cargando los roles disponibles
        [HttpGet]
        public IActionResult Crear()
        {

            CargarRoles();
            return View(new UsuarioCrearViewModel());
        }

        // Método para crear un nuevo usuario, con validaciones y manejo de errores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioCrearViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CargarRoles();
                return View(model);
            }

            model.Correo = model.Correo.Trim().ToLower();
            model.NombreCompleto = model.NombreCompleto.Trim();

            var usuarioExistente = await _userManager.FindByEmailAsync(model.Correo);
            if (usuarioExistente != null)
            {
                ModelState.AddModelError(nameof(model.Correo), "Ya existe un usuario con ese correo.");
                CargarRoles();
                return View(model);
            }

            var usuario = new UsuarioSistema
            {
                UserName = model.Correo,
                Email = model.Correo,
                NombreCompleto = model.NombreCompleto,
                EmailConfirmed = true,
                Activo = model.Activo
            };

            var resultado = await _userManager.CreateAsync(usuario, model.Contrasena);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                CargarRoles();
                return View(model);
            }

            var rolExiste = await _roleManager.RoleExistsAsync(model.Rol);
            if (!rolExiste)
            {
                ModelState.AddModelError(nameof(model.Rol), "El rol seleccionado no existe.");
                await _userManager.DeleteAsync(usuario);
                CargarRoles();
                return View(model);
            }

            var resultadoRol = await _userManager.AddToRoleAsync(usuario, model.Rol);

            if (!resultadoRol.Succeeded)
            {
                foreach (var error in resultadoRol.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await _userManager.DeleteAsync(usuario);
                CargarRoles();
                return View(model);
            }

            await RegistrarAuditoriaAsync(
                "Crear usuario",
                usuario,
                $"Se creó el usuario con rol {model.Rol} y estado activo = {model.Activo}.");

            TempData["MensajeExito"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Método para cambiar el estado activo/inactivo de un usuario, con validaciones para evitar autodesactivación y desactivación del último administrador activo
        [HttpGet]
        public async Task<IActionResult> CambiarEstado(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
                return NotFound();

            var usuarioActualId = _userManager.GetUserId(User);

            // No permitir autodesactivación
            if (usuario.Id == usuarioActualId && usuario.Activo)
            {
                TempData["MensajeError"] = "No puedes desactivar tu propio usuario.";
                return RedirectToAction(nameof(Index));
            }

            // No permitir desactivar al último administrador activo
            if (usuario.Activo && await _userManager.IsInRoleAsync(usuario, "Administrador"))
            {
                var totalAdminsActivos = await ContarAdministradoresActivosAsync();

                if (totalAdminsActivos <= 1)
                {
                    TempData["MensajeError"] = "No puedes desactivar al último administrador activo del sistema.";
                    return RedirectToAction(nameof(Index));
                }
            }

            usuario.Activo = !usuario.Activo;

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                TempData["MensajeError"] = "No se pudo cambiar el estado del usuario.";
                return RedirectToAction(nameof(Index));
            }

            await RegistrarAuditoriaAsync(
                usuario.Activo ? "Activar usuario" : "Desactivar usuario",
                usuario,
                usuario.Activo ? "El usuario fue activado." : "El usuario fue desactivado.");

            TempData["MensajeExito"] = usuario.Activo
                ? "Usuario activado correctamente."
                : "Usuario desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Método para mostrar el formulario de edición de un usuario, cargando su información actual y los roles disponibles
        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(usuario);
            var rolActual = roles.FirstOrDefault() ?? string.Empty;

            var model = new UsuarioEditarViewModel
            {
                Id = usuario.Id,
                Correo = usuario.Email ?? string.Empty,
                NombreCompleto = usuario.NombreCompleto,
                Rol = rolActual,
                Activo = usuario.Activo
            };

            CargarRoles();
            return View(model);
        }

        //  Método para editar un usuario, con validaciones para evitar autodesactivación y desactivación del último administrador activo, además de manejar cambios de rol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(UsuarioEditarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CargarRoles();
                return View(model);
            }

            model.NombreCompleto = model.NombreCompleto.Trim();

            var usuario = await _userManager.FindByIdAsync(model.Id);

            if (usuario == null)
                return NotFound();

            var usuarioActualId = _userManager.GetUserId(User);
            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            var rolActual = rolesActuales.FirstOrDefault() ?? string.Empty;
            var esAdminActual = rolActual == "Administrador";
            var seguiraSiendoAdmin = model.Rol == "Administrador";

            if (usuario.Id == usuarioActualId && !model.Activo)
            {
                ModelState.AddModelError(string.Empty, "No puedes desactivar tu propio usuario.");
                CargarRoles();
                return View(model);
            }

            if (esAdminActual)
            {
                var totalAdminsActivos = await ContarAdministradoresActivosAsync();

                var intentaraQuitarUltimoAdmin =
                    totalAdminsActivos <= 1 &&
                    (!model.Activo || !seguiraSiendoAdmin);

                if (intentaraQuitarUltimoAdmin)
                {
                    ModelState.AddModelError(string.Empty, "No puedes desactivar o cambiar el rol del último administrador activo del sistema.");
                    CargarRoles();
                    return View(model);
                }
            }

            usuario.NombreCompleto = model.NombreCompleto;
            usuario.Activo = model.Activo;

            var resultadoUpdate = await _userManager.UpdateAsync(usuario);

            if (!resultadoUpdate.Succeeded)
            {
                foreach (var error in resultadoUpdate.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                CargarRoles();
                return View(model);
            }

            if (rolActual != model.Rol)
            {
                if (!string.IsNullOrWhiteSpace(rolActual))
                {
                    var removeResult = await _userManager.RemoveFromRoleAsync(usuario, rolActual);
                    if (!removeResult.Succeeded)
                    {
                        foreach (var error in removeResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        CargarRoles();
                        return View(model);
                    }
                }

                var addResult = await _userManager.AddToRoleAsync(usuario, model.Rol);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    CargarRoles();
                    return View(model);
                }
            }

            await RegistrarAuditoriaAsync(
                "Editar usuario",
                usuario,
                $"Se actualizó el usuario. Nuevo nombre: {usuario.NombreCompleto}, nuevo rol: {model.Rol}, activo: {usuario.Activo}.");

            TempData["MensajeExito"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para cargar los roles disponibles
        private void CargarRoles()
        {
            ViewBag.Roles = _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name!,
                    Text = r.Name!
                })
                .ToList();
        }

        // Método para contar el número de administradores activos en el sistema
        private async Task<int> ContarAdministradoresActivosAsync()
        {
            var usuarios = _userManager.Users.ToList();
            var total = 0;

            foreach (var usuario in usuarios)
            {
                if (!usuario.Activo)
                    continue;

                if (await _userManager.IsInRoleAsync(usuario, "Administrador"))
                {
                    total++;
                }
            }

            return total;
        }

        // Método para mostrar el formulario de restablecimiento de contraseña de un usuario, cargando su información básica
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var usuarioActualId = _userManager.GetUserId(User);

            if (id == usuarioActualId)
            {
                TempData["MensajeError"] = "No puedes restablecer tu propia contraseña desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
                return NotFound();

            var model = new UsuarioResetPasswordViewModel
            {
                Id = usuario.Id,
                Correo = usuario.Email ?? string.Empty
            };

            return View(model);
        }

        // Método para restablecer la contraseña de un usuario, generando un token de restablecimiento y aplicando la nueva contraseña, con manejo de errores

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UsuarioResetPasswordViewModel model)
        {
            var usuarioActualId = _userManager.GetUserId(User);

            if (model.Id == usuarioActualId)
            {
                TempData["MensajeError"] = "No puedes restablecer tu propia contraseña desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.FindByIdAsync(model.Id);

            if (usuario == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await _userManager.ResetPasswordAsync(usuario, token, model.NuevaContrasena);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await RegistrarAuditoriaAsync(
                "Resetear contraseña",
                usuario,
                "Se restableció la contraseña del usuario.");

            TempData["MensajeExito"] = "Contraseña restablecida correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Método para registrar una acción de auditoría relacionada con la gestión de usuarios, guardando información relevante en la base de datos
        private async Task RegistrarAuditoriaAsync(string accion, UsuarioSistema usuarioAfectado, string detalle)
        {
            var adminActual = await _userManager.GetUserAsync(User);

            if (adminActual == null)
                return;

            var auditoria = new AuditoriaUsuario
            {
                Fecha = DateTime.UtcNow,
                AdministradorId = adminActual.Id,
                AdministradorCorreo = adminActual.Email ?? string.Empty,
                Accion = accion,
                UsuarioAfectadoId = usuarioAfectado.Id,
                UsuarioAfectadoCorreo = usuarioAfectado.Email ?? string.Empty,
                Detalle = detalle
            };

            _context.AuditoriasUsuario.Add(auditoria);
            await _context.SaveChangesAsync();
        }
    }
}