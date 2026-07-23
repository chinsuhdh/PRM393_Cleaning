namespace Cleaning.BLL.Constants;

public static class JwtConfigKeys
{
    public const string Secret = "JwtConfig:Secret";
    public const string Issuer = "JwtConfig:Issuer";
    public const string Audience = "JwtConfig:Audience";
    public const string AccessTokenExpirationMinutes = "JwtConfig:AccessTokenExpirationMinutes";
    public const string RefreshTokenExpirationDays = "JwtConfig:RefreshTokenExpirationDays";
}
