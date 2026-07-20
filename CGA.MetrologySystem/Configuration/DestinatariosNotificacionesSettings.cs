namespace CGA.MetrologySystem.Configuration
{
    public class DestinatariosNotificacionesSettings
    {
        public bool ModoPreproduccion { get; set; }
        public bool PermitirCorreosRegistrados { get; set; } = true;
        public string AdministradorSistema { get; set; } = string.Empty;
        public string AdministradorMetrologico { get; set; } = string.Empty;
    }
}
