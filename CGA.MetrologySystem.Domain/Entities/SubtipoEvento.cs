using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class SubtipoEvento
    {
        public int SubtipoEventoId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<EventoMetrologico> EventosMetrologicos { get; set; } = new List<EventoMetrologico>();
    }
}
