using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class EventoMantenimientoActividad
    {
        public int EventoMantenimientoActividadId { get; set; }

        public int EventoMetrologicoId { get; set; }

        public string DescripcionActividad { get; set; } = string.Empty;
        public string? Observaciones { get; set; }

        public int Orden { get; set; }

        public EventoMetrologico EventoMetrologico { get; set; } = null!;
    }
}