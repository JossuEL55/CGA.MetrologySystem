using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Application.Services;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CGA.MetrologySystem.Infrastructure.Services;
using QuestPDF.Infrastructure;
using CGA.MetrologySystem.Services.Pdf;

QuestPDF.Settings.License = LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<GoogleDriveSettings>(
    builder.Configuration.GetSection("GoogleDriveSettings"));

builder.Services.Configure<GoogleOAuthSettings>(
    builder.Configuration.GetSection("GoogleOAuthSettings"));

builder.Services.Configure<GoogleOAuthTokenStorageSettings>(
    builder.Configuration.GetSection("GoogleOAuthTokenStorageSettings"));

builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IGoogleDriveCredentialProvider, GoogleDriveCredentialProvider>();
builder.Services.AddScoped<MantenimientoPdfService>();
builder.Services.AddScoped<VerificacionPdfService>();

builder.Services.AddDefaultIdentity<UsuarioSistema>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<SignInManager<UsuarioSistema>, SignInManagerPersonalizado>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.Name = "CGA.Auth";

    options.Events.OnValidatePrincipal = async context =>
    {
        if (context.Principal == null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<UsuarioSistema>>();
        var user = await userManager.GetUserAsync(context.Principal);

        if (user == null || !user.Activo)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await IdentitySeeder.SeedRolesAndAdminAsync(services);
}

app.Run();