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
        public DbSet<PlantillaEvento> PlantillasEvento { get; set; }
        public DbSet<PlantillaEventoItem> PlantillasEventoItem { get; set; }
        public DbSet<EventoMantenimientoDato> EventosMantenimientoDato { get; set; }
        public DbSet<EventoVerificacionDato> EventosVerificacionDato { get; set; }
        public DbSet<EvidenciaEventoMetrologico> EvidenciasEventoMetrologico { get; set; }
        public DbSet<FichaTecnicaEquipo> FichasTecnicasEquipo { get; set; }
        public DbSet<HojaVidaEquipo> HojasVidaEquipo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EvidenciaEventoMetrologico>(entity =>
            {
                entity.HasKey(e => e.EvidenciaEventoMetrologicoId);

                entity.Property(e => e.NombreArchivo)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.ContentType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.GoogleDriveFileId)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.RutaArchivo)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.TipoEvidencia)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.HasOne(e => e.EventoMetrologico)
                    .WithMany(e => e.Evidencias)
                    .HasForeignKey(e => e.EventoMetrologicoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<FichaTecnicaEquipo>(entity =>
            {
                entity.HasKey(f => f.FichaTecnicaEquipoId);

                entity.Property(f => f.NombreArchivoPdf)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(f => f.GoogleDriveFileId)
                    .HasMaxLength(255);

                entity.Property(f => f.RutaPdf)
                    .HasMaxLength(500);

                entity.HasOne(f => f.Equipo)
                    .WithOne(e => e.FichaTecnica)
                    .HasForeignKey<FichaTecnicaEquipo>(f => f.EquipoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(f => f.EquipoId)
                    .IsUnique();
            });
            modelBuilder.Entity<HojaVidaEquipo>(entity =>
            {
                entity.HasKey(h => h.HojaVidaEquipoId);

                entity.Property(h => h.NombreArchivoPdf)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(h => h.GoogleDriveFileId)
                    .HasMaxLength(255);

                entity.Property(h => h.RutaPdf)
                    .HasMaxLength(500);

                entity.HasOne(h => h.Equipo)
                    .WithOne(e => e.HojaVida)
                    .HasForeignKey<HojaVidaEquipo>(h => h.EquipoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(h => h.EquipoId)
                    .IsUnique();
            });
        }

    }
}