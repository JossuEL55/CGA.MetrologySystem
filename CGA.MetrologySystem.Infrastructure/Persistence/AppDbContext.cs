using CGA.MetrologySystem.Domain.Entities;
using CGA.MetrologySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CGA.MetrologySystem.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<UsuarioSistema, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        //Revisar las dos entidades siguientes, no se si es necesario tener ambas (IMPORTANTE)
        public DbSet<AuditoriaUsuario> AuditoriasUsuario { get; set; }
        public DbSet<TipoEquipo> TiposEquipo { get; set; }
        public DbSet<TipoEventoMetrologico> TiposEventoMetrologico { get; set; }
        public DbSet<TipoDocumento> TiposDocumento { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Ubicacion> Ubicaciones { get; set; }
        public DbSet<ResponsableInterno> ResponsablesInternos { get; set; }
        public DbSet<SubtipoEvento> SubtiposEvento { get; set; }
        public DbSet<TipoMantenimiento> TiposMantenimiento { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Laboratorio> Laboratorios { get; set; }
        public DbSet<CaracteristicaMetrologicaEquipo> CaracteristicasMetrologicasEquipo { get; set; }
        public DbSet<ConfiguracionControlEquipo> ConfiguracionesControlEquipo { get; set; }
        public DbSet<EventoMetrologico> EventosMetrologicos { get; set; }
        public DbSet<EventoVerificacionResultado> EventosVerificacionResultado { get; set; }
        public DbSet<EventoMantenimientoActividad> EventosMantenimientoActividad { get; set; }
        public DbSet<EventoCalibracionDato> EventosCalibracionDato { get; set; }
        public DbSet<GoogleDriveCredential> GoogleDriveCredentials { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}