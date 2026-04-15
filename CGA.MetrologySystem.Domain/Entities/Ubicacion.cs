using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class Ubicacion
    {
        public int UbicacionId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();

    }
}