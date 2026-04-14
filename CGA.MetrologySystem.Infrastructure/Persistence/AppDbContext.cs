using CGA.MetrologySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CGA.MetrologySystem.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoEquipo> TiposEquipo { get; set; }
        public DbSet<TipoEventoMetrologico> TiposEventoMetrologico { get; set; }
        public DbSet<TipoDocumento> TiposDocumento { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Ubicacion> Ubicaciones { get; set; }
        public DbSet<ResponsableInterno> ResponsablesInternos { get; set; }
        public DbSet<SubtipoEvento> SubtiposEvento { get; set; }
        public DbSet<TipoMantenimiento> TiposMantenimiento { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}