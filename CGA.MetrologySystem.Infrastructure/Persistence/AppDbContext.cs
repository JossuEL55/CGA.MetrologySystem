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
        public DbSet<AlertaEnviada> AlertasEnviadas { get; set; }
        public DbSet<NotificacionEnviada> NotificacionesEnviadas { get; set; }
        public DbSet<AuditoriaUsuario> AuditoriasUsuario { get; set; }
        public DbSet<AuditoriaMetrologica> AuditoriasMetrologicas { get; set; }
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

            modelBuilder.Entity<EventoVerificacionResultado>(entity =>
            {
                entity.Property(e => e.EvidenciaNombreArchivo)
                    .HasMaxLength(255);

                entity.Property(e => e.EvidenciaContentType)
                    .HasMaxLength(100);

                entity.Property(e => e.EvidenciaGoogleDriveFileId)
                    .HasMaxLength(255);

                entity.Property(e => e.EvidenciaRutaArchivo)
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<EventoMantenimientoActividad>(entity =>
            {
                entity.Property(e => e.EvidenciaNombreArchivo)
                    .HasMaxLength(255);

                entity.Property(e => e.EvidenciaContentType)
                    .HasMaxLength(100);

                entity.Property(e => e.EvidenciaGoogleDriveFileId)
                    .HasMaxLength(255);

                entity.Property(e => e.EvidenciaRutaArchivo)
                    .HasMaxLength(500);
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
            modelBuilder.Entity<Equipo>(entity =>
            {
                entity.Property(e => e.FotoNombreArchivo)
                    .HasMaxLength(255);

                entity.Property(e => e.FotoGoogleDriveFileId)
                    .HasMaxLength(255);

                entity.Property(e => e.FotoRutaArchivo)
                    .HasMaxLength(500);
            });
            modelBuilder.Entity<AlertaEnviada>(entity =>
            {
                entity.HasKey(a => a.AlertaEnviadaId);

                entity.Property(a => a.TipoEvento)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(a => a.TipoAlerta)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(a => a.Mensaje)
                    .HasMaxLength(500);

                entity.HasOne(a => a.Equipo)
                    .WithMany()
                    .HasForeignKey(a => a.EquipoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<NotificacionEnviada>(entity =>
            {
                entity.HasKey(n => n.NotificacionEnviadaId);

                entity.Property(n => n.TipoNotificacion)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(n => n.TipoEvento)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(n => n.Mensaje)
                    .HasMaxLength(500);

                entity.HasOne(n => n.Equipo)
                    .WithMany()
                    .HasForeignKey(n => n.EquipoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.EventoMetrologico)
                    .WithMany()
                    .HasForeignKey(n => n.EventoMetrologicoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<EventoMetrologico>(entity =>
            {
                entity.Property(e => e.ObservacionCargaHistorica)
                    .HasMaxLength(500);
            });
            modelBuilder.Entity<AuditoriaMetrologica>(entity =>
            {
                entity.HasKey(a => a.AuditoriaMetrologicaId);

                entity.Property(a => a.UsuarioId)
                    .HasMaxLength(450);

                entity.Property(a => a.UsuarioNombre)
                    .HasMaxLength(180)
                    .IsRequired();

                entity.Property(a => a.UsuarioCorreo)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(a => a.RolUsuario)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(a => a.Accion)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(a => a.Entidad)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(a => a.EntidadId)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(a => a.CodigoEquipo)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(a => a.NombreEquipo)
                    .HasMaxLength(220)
                    .IsRequired();

                entity.Property(a => a.TipoEvento)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(a => a.Detalle)
                    .HasMaxLength(1000)
                    .IsRequired();
            });

        }

    }
}
