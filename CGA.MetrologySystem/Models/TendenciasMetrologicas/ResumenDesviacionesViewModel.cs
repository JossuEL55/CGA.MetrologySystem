namespace CGA.MetrologySystem.Models.TendenciasMetrologicas
{
    public class ResumenDesviacionesViewModel
    {
        public int EquiposAnalizados { get; set; }

        public int EventosAnalizados { get; set; }

        public double DesviacionPromedioGlobal { get; set; }

        public int EventosTardios { get; set; }

        public int EventosAnticipados { get; set; }

        public int EventosATiempo { get; set; }

        public int EventosExtraordinarios { get; set; }

        public int MayorDesviacion { get; set; }
    }
}
