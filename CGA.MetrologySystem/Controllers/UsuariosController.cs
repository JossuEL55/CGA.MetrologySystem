using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Models.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Notificaciones;
using System.Net;

namespace CGA.MetrologySystem.Controllers
{
    // Controlador para la gestión de usuarios, accesible solo para administradores
    [Authorize(Roles = RolesSistema.AdministracionUsuarios)]
    public class UsuariosController : Controller
    {
        private readonly UserManager<UsuarioSistema> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly INotificacionSeguridadService _notificacionSeguridadService;
        private readonly ILogger<UsuariosController> _logger;

        //Constructor 
        public UsuariosController(
               UserManager<UsuarioSistema> userManager,
               RoleManager<IdentityRole> roleManager,
               AppDbContext context,
               IEmailService emailService,
               IEmailTemplateService emailTemplateService,
               INotificacionSeguridadService notificacionSeguridadService,
               ILogger<UsuariosController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _notificacionSeguridadService = notificacionSeguridadService;
            _logger = logger;
        }

        // Método para listar todos los usuarios con su información básica y rol asignado
        public async Task<IActionResult> Index(string? busqueda, string? rol, string? estado)
        {
            var usuarios = _userManager.Users
                .OrderBy(u => u.NombreCompleto)
                .ToList();
            var listaUsuarios = new List<UsuarioListadoViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                var rolCodigo = ObtenerRolPrincipal(roles);

                listaUsuarios.Add(new UsuarioListadoViewModel
                {
                    Id = usuario.Id,
                    Correo = usuario.Email ?? string.Empty,
                    NombreCompleto = usuario.NombreCompleto,
                    Activo = usuario.Activo,
                    Rol = RolesSistema.ObtenerNombreVisible(rolCodigo),
                    RolCodigo = rolCodigo,
                    FechaCreacionTexto = FormatearFecha(usuario.FechaCreacion),
                    UltimoAccesoTexto = FormatearFecha(usuario.UltimoAcceso)
                });
            }

            var usuariosFiltrados = AplicarFiltros(listaUsuarios, busqueda, rol, estado);

            var model = new UsuariosIndexViewModel
            {
                TotalUsuarios = listaUsuarios.Count,
                UsuariosActivos = listaUsuarios.Count(u => u.Activo),
                UsuariosInactivos = listaUsuarios.Count(u => !u.Activo),
                TotalAdministradores = listaUsuarios.Count(u =>
                    RolesSistema.EsAdministradorSistema(u.RolCodigo)),
                TotalAdministradoresMetrologicos = listaUsuarios.Count(u =>
                    RolesSistema.EsAdministradorMetrologico(u.RolCodigo)),
                TotalTecnicos = listaUsuarios.Count(u => u.RolCodigo == RolesSistema.Tecnico),
                Busqueda = busqueda,
                Rol = rol,
                Estado = estado,
                Usuarios = usuariosFiltrados
            };

            CargarRoles();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(usuario);
            var rolCodigo = ObtenerRolPrincipal(roles);

            var model = new UsuarioListadoViewModel
            {
                Id = usuario.Id,
                Correo = usuario.Email ?? string.Empty,
                NombreCompleto = usuario.NombreCompleto,
                Activo = usuario.Activo,
                Rol = RolesSistema.ObtenerNombreVisible(rolCodigo),
                RolCodigo = rolCodigo,
                FechaCreacionTexto = FormatearFecha(usuario.FechaCreacion),
                UltimoAccesoTexto = FormatearFecha(usuario.UltimoAcceso)
            };

            return View(model);
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
                Activo = model.Activo,
                FechaCreacion = DateTime.UtcNow,
                DebeCambiarContrasena = true
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

            var correoEnviado = await EnviarCorreoBienvenidaAsync(usuario, model.Rol);

            TempData["MensajeExito"] = correoEnviado
                ? "Usuario creado correctamente. Se envió el correo de bienvenida."
                : "Usuario creado correctamente. No se pudo enviar el correo de bienvenida; revisa la configuración SMTP o el correo del usuario.";

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
            if (usuario.Activo && await EsAdministradorSistemaAsync(usuario))
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
            var rolActual = ObtenerRolPrincipal(roles);

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
            var rolActual = ObtenerRolPrincipal(rolesActuales);
            var esAdminActual = RolesSistema.EsAdministradorSistema(rolActual);
            var seguiraSiendoAdmin = model.Rol == RolesSistema.AdministradorSistema;

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

            var rolesParaRemover = rolesActuales
                .Where(r => r != model.Rol)
                .ToArray();

            if (rolesParaRemover.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(usuario, rolesParaRemover);
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

            if (!rolesActuales.Contains(model.Rol))
            {
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
                .Where(r => r.Name != "Administrador")
                .Select(r => new SelectListItem
                {
                    Value = r.Name!,
                    Text = RolesSistema.ObtenerNombreVisible(r.Name!)
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

                if (await EsAdministradorSistemaAsync(usuario))
                {
                    total++;
                }
            }

            return total;
        }

        private async Task<bool> EsAdministradorSistemaAsync(UsuarioSistema usuario)
        {
            return await _userManager.IsInRoleAsync(usuario, RolesSistema.AdministradorSistema);
        }

        private static string ObtenerRolPrincipal(IList<string> roles)
        {
            if (roles.Contains(RolesSistema.AdministradorSistema))
                return RolesSistema.AdministradorSistema;

            if (roles.Contains(RolesSistema.AdministradorMetrologico))
                return RolesSistema.AdministradorMetrologico;

            if (roles.Contains(RolesSistema.Tecnico))
                return RolesSistema.Tecnico;

            return roles.FirstOrDefault() ?? "Sin rol";
        }

        private static List<UsuarioListadoViewModel> AplicarFiltros(
            List<UsuarioListadoViewModel> usuarios,
            string? busqueda,
            string? rol,
            string? estado)
        {
            var consulta = usuarios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim();
                consulta = consulta.Where(u =>
                    u.NombreCompleto.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                    u.Correo.Contains(termino, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(rol))
            {
                consulta = consulta.Where(u => u.RolCodigo == rol);
            }

            if (estado == "activo")
            {
                consulta = consulta.Where(u => u.Activo);
            }
            else if (estado == "inactivo")
            {
                consulta = consulta.Where(u => !u.Activo);
            }

            return consulta
                .OrderByDescending(u => u.Activo)
                .ThenBy(u => u.NombreCompleto)
                .ToList();
        }

        private static string FormatearFecha(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                return "No registrado";
            }

            return fecha.Value
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm");
        }

        private async Task<bool> EnviarCorreoBienvenidaAsync(UsuarioSistema usuario, string rol)
        {
            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                return false;
            }

            try
            {
                var urlSistema = Url.Action("Login", "Auth", null, Request.Scheme)
                    ?? $"{Request.Scheme}://{Request.Host}";
                var nombre = WebUtility.HtmlEncode(usuario.NombreCompleto);
                var correo = WebUtility.HtmlEncode(usuario.Email);
                var rolVisible = WebUtility.HtmlEncode(RolesSistema.ObtenerNombreVisible(rol));
                var url = WebUtility.HtmlEncode(urlSistema);

                var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
                {
                    new EmailTemplateRow("Correo de acceso", usuario.Email ?? string.Empty),
                    new EmailTemplateRow("Rol asignado", RolesSistema.ObtenerNombreVisible(rol)),
                    new EmailTemplateRow("Acceso al sistema", urlSistema)
                });

                var cuerpo = _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
                {
                    Titulo = "Bienvenido a CGA Metrology System",
                    Preheader = "Tu cuenta de acceso ha sido creada correctamente.",
                    Etiqueta = "Gestion de usuarios",
                    Nivel = "exito",
                    ContenidoHtml = $@"
                        <p style=""margin:0 0 14px;"">Hola <strong>{nombre}</strong>,</p>
                        <p style=""margin:0 0 14px;"">La administración del sistema registró tu cuenta para acceder a la plataforma de trazabilidad y control metrológico de CGA.</p>
                        {tabla}
                        <p style=""margin:0;"">La contraseña inicial fue definida por administración. Por seguridad, el sistema solicitará cambiarla durante el ingreso correspondiente.</p>",
                    TextoBoton = "Ingresar al sistema",
                    UrlBoton = urlSistema
                });

                await _emailService.EnviarCorreoAsync(
                    usuario.Email!,
                    "Cuenta creada - CGA Metrology System",
                    cuerpo);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo enviar el correo de bienvenida al usuario {UsuarioId}.",
                    usuario.Id);

                return false;
            }
        }

        private async Task<bool> EnviarCorreoResetPasswordAsync(UsuarioSistema usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                return false;
            }

            try
            {
                var urlSistema = Url.Action("Login", "Auth", null, Request.Scheme)
                    ?? $"{Request.Scheme}://{Request.Host}";
                var nombre = WebUtility.HtmlEncode(usuario.NombreCompleto);

                var tabla = _emailTemplateService.ConstruirTablaDatos(new[]
                {
                    new EmailTemplateRow("Cuenta", usuario.Email),
                    new EmailTemplateRow("Acceso al sistema", urlSistema)
                });

                var cuerpo = _emailTemplateService.ConstruirCorreo(new EmailTemplateModel
                {
                    Titulo = "Contraseña restablecida",
                    Preheader = "Se actualizó el acceso de tu cuenta en CGA Metrology System.",
                    Etiqueta = "Gestion de usuarios",
                    Nivel = "advertencia",
                    ContenidoHtml = $@"
                        <p style=""margin:0 0 14px;"">Hola <strong>{nombre}</strong>,</p>
                        <p style=""margin:0 0 14px;"">La administración del sistema restableció la contraseña asociada a tu cuenta.</p>
                        {tabla}
                        <p style=""margin:0;"">Por seguridad, ingresa con la contraseña definida por administración y cámbiala desde tu perfil cuando accedas al sistema.</p>",
                    TextoBoton = "Ingresar al sistema",
                    UrlBoton = urlSistema
                });

                await _emailService.EnviarCorreoAsync(
                    usuario.Email!,
                    "Contraseña restablecida - CGA Metrology System",
                    cuerpo);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo enviar el correo de restablecimiento de contraseña al usuario {UsuarioId}.",
                    usuario.Id);

                return false;
            }
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

            usuario.DebeCambiarContrasena = true;
            var resultadoUpdate = await _userManager.UpdateAsync(usuario);

            if (!resultadoUpdate.Succeeded)
            {
                foreach (var error in resultadoUpdate.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await RegistrarAuditoriaAsync(
                "Resetear contraseña",
                usuario,
                "Se restableció la contraseña del usuario.");

            var administradorActual = await _userManager.GetUserAsync(User);
            var correoUsuarioEnviado = await EnviarCorreoResetPasswordAsync(usuario);
            var alertaSeguridadEnviada = await _notificacionSeguridadService
                .NotificarRestablecimientoContrasenaAsync(
                    usuario,
                    administradorActual);

            TempData["MensajeExito"] = correoUsuarioEnviado && alertaSeguridadEnviada
                ? "Contraseña restablecida correctamente. Se enviaron los avisos al usuario y al Administrador del Sistema."
                : "Contraseña restablecida correctamente. Uno o más avisos por correo no pudieron enviarse.";

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
