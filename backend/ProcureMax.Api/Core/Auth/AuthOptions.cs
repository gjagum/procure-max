namespace ProcureMax.Core.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public string Issuer { get; set; } = "ProcureMax";
    public string Audience { get; set; } = "ProcureMax";
    public string SigningSecret { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
