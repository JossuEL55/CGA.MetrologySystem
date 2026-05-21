using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CGA.MetrologySystem.Domain.Entities
{
    public class Equipo
    {
        public int EquipoId { get; set; }

        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;

        public int TipoEquipoId { get; set; }
        public int ProveedorId { get; set; }
        public int UbicacionId { get; set; }
        public int ResponsableInternoId { get; set; }

        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Serie { get; set; }
        public string? Identificacion { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaAdquisicion { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaPuestaFuncionamiento { get; set; }
        public string? FabricanteLugarOrigen { get; set; }
        public string? CatalogoManejoOperacion { get; set; }
        public string? MantenimientoFabricante { get; set; }
        public string? CondicionesOperacion { get; set; }
        public bool Activo { get; set; } = true;

        public string? GoogleDriveFolderId { get; set; }
        public string? FotoNombreArchivo { get; set; }
        public string? FotoGoogleDriveFileId { get; set; }
        public string? FotoRutaArchivo { get; set; }
        public TipoEquipo TipoEquipo { get; set; } = null!;
        public Proveedor Proveedor { get; set; } = null!;
        public Ubicacion Ubicacion { get; set; } = null!;
        public ResponsableInterno ResponsableInterno { get; set; } = null!;

        public ICollection<CaracteristicaMetrologicaEquipo> CaracteristicasMetrologicas { get; set; } = new List<CaracteristicaMetrologicaEquipo>();
        public ICollection<ConfiguracionControlEquipo> ConfiguracionesControl { get; set; } = new List<ConfiguracionControlEquipo>();
        public ICollection<EventoMetrologico> EventosMetrologicos { get; set; } = new List<EventoMetrologico>();
        public FichaTecnicaEquipo? FichaTecnica { get; set; }
        public HojaVidaEquipo? HojaVida { get; set; }
    }
}
