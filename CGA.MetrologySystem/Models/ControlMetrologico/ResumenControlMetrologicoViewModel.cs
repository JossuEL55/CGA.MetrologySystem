namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public class ResumenControlMetrologicoViewModel
    {
        public int TotalEquipos { get; set; }

        public int Vigentes { get; set; }

        public int ProximosAVencer { get; set; }

        public int Vencidos { get; set; }

        public int SinEventos { get; set; }

        public int SinConfiguracion { get; set; }

        public int NoRequierenControl { get; set; }
    }
}