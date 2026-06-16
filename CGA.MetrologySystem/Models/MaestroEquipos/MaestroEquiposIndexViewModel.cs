namespace CGA.MetrologySystem.Models.MaestroEquipos
{
    public class MaestroEquiposIndexViewModel
    {
        public MaestroEquiposFiltroViewModel Filtros { get; set; } = new();

        public List<MaestroEquipoItemViewModel> Equipos { get; set; } = new();

        public int TotalEquipos { get; set; }

        public int TotalConConfiguracionIncompleta { get; set; }

        public int TotalVencidos { get; set; }

        public int TotalProximosAVencer { get; set; }
    }
}
