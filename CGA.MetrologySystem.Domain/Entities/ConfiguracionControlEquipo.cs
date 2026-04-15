using System;
using System.Collections.Generic;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class ConfiguracionControlEquipo
    {
        public int ConfiguracionControlEquipoId { get; set; }

        public int EquipoId { get; set; }
        public int TipoEventoMetrologicoId { get; set; }

        public int? PeriodicidadValor { get; set; }
        public string? PeriodicidadUnidad { get; set; }

        public bool RequiereControl { get; set; } = true;
        public bool PermitePorIngreso { get; set; } = false;
        public bool Activo { get; set; } = true;

        public Equipo Equipo { get; set; } = null!;
        public TipoEventoMetrologico TipoEventoMetrologico { get; set; } = null!;
    }
}