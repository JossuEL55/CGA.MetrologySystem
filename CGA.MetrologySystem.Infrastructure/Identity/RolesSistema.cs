namespace CGA.MetrologySystem.Infrastructure.Identity
{
    public static class RolesSistema
    {
        public const string AdministradorSistema = "AdministradorSistema";
        public const string AdministradorMetrologico = "AdministradorMetrologico";
        public const string Tecnico = "Tecnico";

        public const string AdministracionUsuarios = AdministradorSistema;
        public const string GestionMetrologica = AdministradorMetrologico;
        public const string SupervisionMetrologica = AdministradorSistema + "," + AdministradorMetrologico;
        public const string OperacionMetrologica = AdministradorMetrologico + "," + Tecnico;
        public const string TodosOperativos = AdministradorSistema + "," + AdministradorMetrologico + "," + Tecnico;

        public static readonly string[] RolesBase =
        {
            AdministradorSistema,
            AdministradorMetrologico,
            Tecnico
        };

        public static bool EsAdministradorMetrologico(string rol)
        {
            return rol == AdministradorMetrologico;
        }

        public static bool EsAdministradorSistema(string rol)
        {
            return rol == AdministradorSistema;
        }

        public static string ObtenerNombreVisible(string rol)
        {
            return rol switch
            {
                AdministradorSistema => "Administrador del Sistema",
                AdministradorMetrologico => "Administrador Metrologico",
                Tecnico => "Tecnico",
                _ => rol
            };
        }
    }
}
