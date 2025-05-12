namespace CertEmpire.Helpers.JwtSettings
{
    public class JwtSetting
    {
        public string Secret { get; set; }
        public int ExpirationMinutes { get; set; }
    }
}
