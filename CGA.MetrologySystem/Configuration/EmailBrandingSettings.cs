namespace CGA.MetrologySystem.Configuration
{
    public class EmailBrandingSettings
    {
        public string PublicBaseUrl { get; set; } = string.Empty;
        public string LogoPath { get; set; } = "/images/logo.png";
        public string EmbeddedLogoContentId { get; set; } = "cga-logo";
        public string SystemName { get; set; } = "CGA Metrology System";
        public string CompanyName { get; set; } = "CGA-OIL Inspection Services S.A.S.";
    }
}
