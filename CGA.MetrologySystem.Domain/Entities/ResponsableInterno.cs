using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class ResponsableInterno
    {
        public int ResponsableInternoId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}
