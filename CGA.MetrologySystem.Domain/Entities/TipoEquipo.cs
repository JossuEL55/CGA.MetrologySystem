using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class TipoEquipo
    {
        public int TipoEquipoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
    }
}