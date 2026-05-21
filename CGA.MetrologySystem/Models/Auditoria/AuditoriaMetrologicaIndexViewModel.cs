namespace CGA.MetrologySystem.Models.Auditoria
{
    public class AuditoriaMetrologicaIndexViewModel
    {
        public int TotalRegistros { get; set; }
        public int CambiosCriticos { get; set; }
        public int RegistrosUltimas24Horas { get; set; }
        public List<AuditoriaMetrologicaListadoViewModel> Registros { get; set; } = new();
    }
}
