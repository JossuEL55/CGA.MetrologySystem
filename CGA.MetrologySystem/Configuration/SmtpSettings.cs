namespace CGA.MetrologySystem.Configuration
{
    // Clase para representar la configuración SMTP para el envío de correos electrónicos, incluyendo
    // propiedades como el host, puerto, credenciales y opciones de seguridad
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "CGA Metrology System";
        public bool EnableSsl { get; set; } = true;
    }
}