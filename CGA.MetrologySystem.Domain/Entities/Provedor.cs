using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class Proveedor
    {
        public int ProveedorId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}