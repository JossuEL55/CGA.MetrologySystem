namespace CGA.MetrologySystem.Domain.Entities
{
    public class AlertaEnviada
    {
        public int AlertaEnviadaId { get; set; }

        public int EquipoId { get; set; }
        public Equipo Equipo { get; set; } = null!;

        public string TipoEvento { get; set; } = string.Empty;
        // Calibracion, Verificacion, Mantenimiento

        public string TipoAlerta { get; set; } = string.Empty;
        // 30Dias, 15Dias, 7Dias, Vencido, Extraordinario, DocumentoFaltante

        public DateTime FechaReferencia { get; set; }
        // Fecha del vencimiento/evento que originó la alerta

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        public string? Destinatarios { get; set; }
        // Correos separados por coma

        public string? Mensaje { get; set; }

        public bool FueExitosa { get; set; } = true;
    }
}