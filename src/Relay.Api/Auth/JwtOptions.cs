namespace Relay.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Relay";

    public string Audience { get; set; } = "Relay";

    public int ExpiryMinutes { get; set; } = 480;
}
