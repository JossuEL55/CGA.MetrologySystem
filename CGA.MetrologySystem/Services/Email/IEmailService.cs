namespace CGA.MetrologySystem.Services.Email
{
    // Interfaz para el servicio de correo electrónico, definiendo métodos para enviar correos a uno o varios destinatarios, c
    // on un asunto y cuerpo en formato HTML
    public interface IEmailService
    {
        Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml);

        Task EnviarCorreoAsync(
            IEnumerable<string> destinatarios,
            string asunto,
            string cuerpoHtml);
    }
}