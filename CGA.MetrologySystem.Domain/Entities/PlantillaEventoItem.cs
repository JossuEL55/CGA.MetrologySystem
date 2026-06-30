namespace CGA.MetrologySystem.Domain.Entities
{
    public class PlantillaEventoItem
    {
        public int PlantillaEventoItemId { get; set; }

        public int PlantillaEventoId { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public int Orden { get; set; }

        public bool Activo { get; set; } = true;

        public PlantillaEvento PlantillaEvento { get; set; } = null!;
    }
}