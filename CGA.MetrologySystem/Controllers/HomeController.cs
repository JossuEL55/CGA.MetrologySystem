using CGA.MetrologySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CGA.MetrologySystem.Controllers
{
    //Authorize para manejo de roles
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult SoloAdmin()
        {
            return Content("Acceso permitido solo para administradores");
        }

        [Authorize(Roles = "Administrador,Tecnico")]
        public IActionResult ZonaOperativa()
        {
            return Content("Acceso permitido para administradores y técnicos");
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [AllowAnonymous]

        //Denegar el acceso al técnico a esta vista 
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}