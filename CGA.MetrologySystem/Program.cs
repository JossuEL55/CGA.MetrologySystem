using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Application.Services;
using CGA.MetrologySystem.Infrastructure.Identity;
using CGA.MetrologySystem.Infrastructure.Persistence;
using CGA.MetrologySystem.Infrastructure.Services;
using CGA.MetrologySystem.Services.ControlMetrologico;
using CGA.MetrologySystem.Services.FichasTecnicas;
using CGA.MetrologySystem.Services.HojasVida;
using CGA.MetrologySystem.Services.Pdf;
using CGA.MetrologySystem.Services.Security;
using CGA.MetrologySystem.Configuration;
using CGA.MetrologySystem.Services.Email;
using CGA.MetrologySystem.Services.Alertas;
using CGA.MetrologySystem.Services.Notificaciones;
using CGA.MetrologySystem.Services.Auditoria;
using CGA.MetrologySystem.Services.DashboardMetrologico;
using CGA.MetrologySystem.Services.TendenciasMetrologicas;
using CGA.MetrologySystem.Services.MaestroEquipos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

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
builder.Services.AddScoped<FichaTecnicaPdfService>();
builder.Services.AddScoped<FichaTecnicaEquipoService>();
builder.Services.AddScoped<HojaVidaPdfService>();
builder.Services.AddScoped<HojaVidaEquipoService>();
builder.Services.AddScoped<IMetrologyRulesService, MetrologyRulesService>();
builder.Services.AddScoped<ControlMetrologicoService>();
builder.Services.Configure<SmtpSettings>(
builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<AlertasSettings>(
builder.Configuration.GetSection("AlertasSettings"));
builder.Services.Configure<NotificacionesSettings>(
builder.Configuration.GetSection("NotificacionesSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAlertaMetrologicaService, AlertaMetrologicaService>();
builder.Services.AddScoped<INotificacionMetrologicaService, NotificacionMetrologicaService>();
builder.Services.AddScoped<IAuditoriaMetrologicaService, AuditoriaMetrologicaService>();
builder.Services.AddScoped<DashboardMetrologicoService>();
builder.Services.AddScoped<TendenciasMetrologicasService>();
builder.Services.AddScoped<MaestroEquiposService>();
builder.Services.AddScoped<MaestroEquiposExcelService>();
builder.Services.AddHostedService<AlertasBackgroundService>();

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedCatalogosEquiposAsync(context);
}

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
