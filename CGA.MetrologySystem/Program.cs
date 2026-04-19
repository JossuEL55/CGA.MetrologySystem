using CGA.MetrologySystem.Application.DTOs;
using CGA.MetrologySystem.Application.Interfaces;
using CGA.MetrologySystem.Application.Services;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<GoogleDriveSettings>(
    builder.Configuration.GetSection("GoogleDriveSettings"));

builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();