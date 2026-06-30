namespace CGA.MetrologySystem.Services.Alertas
{
    public class ResultadoProcesamientoAlertas
    {
        public int ReglasEvaluadas { get; set; }
        public int ReglasSinTipoEvento { get; set; }
        public int ControlesEvaluados { get; set; }
        public int AlertasCandidatas { get; set; }
        public int OmitidasPorDuplicado { get; set; }
        public int Enviadas { get; set; }
        public int SinDestinatarios { get; set; }
        public int Errores { get; set; }

        public string CrearMensajeResumen()
        {
            return $"Alertas evaluadas: {ControlesEvaluados} | Candidatas: {AlertasCandidatas} | Enviadas: {Enviadas} | Duplicadas: {OmitidasPorDuplicado} | Sin destinatario: {SinDestinatarios} | Errores: {Errores}";
        }
    }
}
