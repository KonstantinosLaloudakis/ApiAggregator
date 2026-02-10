namespace ApiAggregator.Api.Configuration;

/// <summary>
/// JWT authentication settings
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ApiAggregator";
    public string Audience { get; set; } = "ApiAggregatorUsers";
    public int ExpirationMinutes { get; set; } = 60;
}
