namespace CGA.MetrologySystem.Services.Email
{
    public interface IEmailTemplateService
    {
        string ConstruirCorreo(EmailTemplateModel model);

        string ConstruirTablaDatos(IEnumerable<EmailTemplateRow> filas);

        string ConstruirLista(IEnumerable<string> items);
    }
}
