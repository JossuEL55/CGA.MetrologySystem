using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class EventoMetrologico
    {
        public int EventoMetrologicoId { get; set; }

        public int EquipoId { get; set; }
        public int TipoEventoMetrologicoId { get; set; }
        public int SubtipoEventoId { get; set; }
        public int ResponsableInternoId { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaEvento { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaProxima { get; set; }

        public string? EstadoEquipoResultado { get; set; }
        public string? ComentariosAdicionales { get; set; }

        public bool EsExtraordinario { get; set; } = false;
        public string? JustificacionExtraordinario { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;

        public Equipo Equipo { get; set; } = null!;
        public TipoEventoMetrologico TipoEventoMetrologico { get; set; } = null!;
        public SubtipoEvento SubtipoEvento { get; set; } = null!;
        public ResponsableInterno ResponsableInterno { get; set; } = null!;

        public ICollection<EventoVerificacionResultado> ResultadosVerificacion { get; set; } = new List<EventoVerificacionResultado>();
        public ICollection<EventoMantenimientoActividad> ActividadesMantenimiento { get; set; } = new List<EventoMantenimientoActividad>();
        public EventoCalibracionDato? EventoCalibracionDato { get; set; }
        public EventoMantenimientoDato? EventoMantenimientoDato { get; set; }
        public EventoVerificacionDato? EventoVerificacionDato { get; set; }
    }
}