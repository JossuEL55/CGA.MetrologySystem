namespace CGA.MetrologySystem.Models.ControlMetrologico
{
    public enum EstadoControlMetrologico
    {
        Vigente = 1,
        ProximoAVencer = 2,
        Vencido = 3,
        SinEventos = 4,
        SinConfiguracion = 5,
        NoRequiereControl = 6
    }
}