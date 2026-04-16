using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CGA.MetrologySystem.Models
{
    public class EquipoViewModel
    {
        public int EquipoId { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre del equipo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un tipo de equipo.")]
        [Display(Name = "Tipo de equipo")]
        public int TipoEquipoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una ubicación.")]
        [Display(Name = "Ubicación")]
        public int UbicacionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un responsable interno.")]
        [Display(Name = "Responsable interno")]
        public int ResponsableInternoId { get; set; }

        [Display(Name = "Marca")]
        public string? Marca { get; set; }

        [Display(Name = "Modelo")]
        public string? Modelo { get; set; }

        [Display(Name = "Serie")]
        public string? Serie { get; set; }

        [Display(Name = "Identificación")]
        public string? Identificacion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de adquisición")]
        public DateTime? FechaAdquisicion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de puesta en funcionamiento")]
        public DateTime? FechaPuestaFuncionamiento { get; set; }

        [Display(Name = "Fabricante / Lugar de origen")]
        public string? FabricanteLugarOrigen { get; set; }

        [Display(Name = "Catálogo de manejo u operación")]
        public string? CatalogoManejoOperacion { get; set; }

        [Display(Name = "Mantenimiento indicado por el fabricante")]
        public string? MantenimientoFabricante { get; set; }

        [Display(Name = "Condiciones de operación")]
        public string? CondicionesOperacion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public List<SelectListItem> TiposEquipo { get; set; } = new();
        public List<SelectListItem> Proveedores { get; set; } = new();
        public List<SelectListItem> Ubicaciones { get; set; } = new();
        public List<SelectListItem> ResponsablesInternos { get; set; } = new();
    }
}