using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Http;

namespace CGA.MetrologySystem.Models
{
    public class EquipoViewModel
    {
        public int EquipoId { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [Display(Name = "Nombre del equipo")]
        public string Nombre { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de equipo.")]
        [Display(Name = "Tipo de equipo")]
        public int TipoEquipoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un proveedor.")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una ubicación.")]
        [Display(Name = "Ubicación")]
        public int UbicacionId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un responsable interno.")]
        [Display(Name = "Responsable interno")]
        public int ResponsableInternoId { get; set; }

        [StringLength(50)]
        [Display(Name = "Marca")]
        public string? Marca { get; set; }

        [StringLength(50)]
        [Display(Name = "Modelo")]
        public string? Modelo { get; set; }

        [StringLength(50)]
        [Display(Name = "Serie")]
        public string? Serie { get; set; }

        [StringLength(50)]
        [Display(Name = "Identificación")]
        public string? Identificacion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de adquisición")]
        public DateTime? FechaAdquisicion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de puesta en funcionamiento")]
        public DateTime? FechaPuestaFuncionamiento { get; set; }

        [StringLength(150)]
        [Display(Name = "Fabricante / Lugar de origen")]
        public string? FabricanteLugarOrigen { get; set; }

        [StringLength(300)]
        [Display(Name = "Catálogo de manejo u operación")]
        public string? CatalogoManejoOperacion { get; set; }

        [StringLength(300)]
        [Display(Name = "Mantenimiento indicado por el fabricante")]
        public string? MantenimientoFabricante { get; set; }

        [StringLength(300)]
        [Display(Name = "Condiciones de operación")]
        public string? CondicionesOperacion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Foto del equipo")]
        public IFormFile? FotoEquipo { get; set; }

        public string? FotoActualNombreArchivo { get; set; }
        public string? FotoActualRutaArchivo { get; set; }

        public List<SelectListItem> TiposEquipo { get; set; } = new();
        public List<SelectListItem> Proveedores { get; set; } = new();
        public List<SelectListItem> Ubicaciones { get; set; } = new();
        public List<SelectListItem> ResponsablesInternos { get; set; } = new();
    }
}
