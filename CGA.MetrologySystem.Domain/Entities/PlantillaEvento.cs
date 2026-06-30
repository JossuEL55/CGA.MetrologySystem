namespace CGA.MetrologySystem.Domain.Entities
{
    public class PlantillaEvento
    {
        public int PlantillaEventoId { get; set; }

        public int TipoEquipoId { get; set; }

        public int TipoEventoMetrologicoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public TipoEquipo TipoEquipo { get; set; } = null!;

        public TipoEventoMetrologico TipoEventoMetrologico { get; set; } = null!;

        public ICollection<PlantillaEventoItem> Items { get; set; } = new List<PlantillaEventoItem>();
    }
}