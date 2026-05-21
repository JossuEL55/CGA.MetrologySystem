namespace CGA.MetrologySystem.Domain.Entities
{
    public class NotificacionEnviada
    {
        public int NotificacionEnviadaId { get; set; }

        public int EquipoId { get; set; }
        public Equipo Equipo { get; set; } = null!;

        public int EventoMetrologicoId { get; set; }
        public EventoMetrologico EventoMetrologico { get; set; } = null!;

        public string TipoNotificacion { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;

        public DateTime FechaReferencia { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        public string? Destinatarios { get; set; }
        public string? Mensaje { get; set; }

        public bool FueExitosa { get; set; } = true;
    }
}
