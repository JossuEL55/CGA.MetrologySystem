using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class EventoVerificacionResultado
    {
        public int EventoVerificacionResultadoId { get; set; }

        public int EventoMetrologicoId { get; set; }

        public string DescripcionItem { get; set; } = string.Empty;
        public bool Cumple { get; set; }
        public string? Observaciones { get; set; }

        public int Orden { get; set; }

        public EventoMetrologico EventoMetrologico { get; set; } = null!;
    }
}