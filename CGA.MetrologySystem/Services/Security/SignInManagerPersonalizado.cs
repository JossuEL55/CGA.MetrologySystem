using CGA.MetrologySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CGA.MetrologySystem.Services.Security
{
    // SignInManager personalizado para verificar el estado "Activo" del usuario antes de permitir el inicio de sesión
    public class SignInManagerPersonalizado : SignInManager<UsuarioSistema>
    {
        public SignInManagerPersonalizado(
            UserManager<UsuarioSistema> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<UsuarioSistema> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<UsuarioSistema>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<UsuarioSistema> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override async Task<bool> CanSignInAsync(UsuarioSistema user)
        {
            if (!user.Activo)
            {
                return false;
            }

            return await base.CanSignInAsync(user);
        }
    }
}