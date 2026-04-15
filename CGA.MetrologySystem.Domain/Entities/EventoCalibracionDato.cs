namespace CGA.MetrologySystem.Domain.Entities

{
    public class EventoCalibracionDato
    {
        public int EventoCalibracionDatoId { get; set; }

        public int EventoMetrologicoId { get; set; }

        public string? NumeroCertificado { get; set; }

        public DateTime? FechaCalibracion { get; set; }

        public int? LaboratorioId { get; set; }

        public Laboratorio? Laboratorio { get; set; }

        public string? RutaCertificado { get; set; }
        public string? Observaciones { get; set; }
        public EventoMetrologico EventoMetrologico { get; set; } = null!;

    }

}