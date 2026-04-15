namespace CGA.MetrologySystem.Domain.Entities
{
    public class CaracteristicaMetrologicaEquipo
    {
        public int CaracteristicaMetrologicaEquipoId { get; set; }

        public int EquipoId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Valor { get; set; }
        public string? Unidad { get; set; }
        public int Orden { get; set; }

        public Equipo Equipo { get; set; } = null!;
    }
}
