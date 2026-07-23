namespace Cleaning.BLL.Constants;

public static class AuthConstants
{
    public const int EmailVerificationOtpExpiryMinutes = 15;
    public const int PasswordResetOtpExpiryMinutes = 5;
    public const int PhoneVerificationOtpExpiryMinutes = 5;
    public const int ReauthTokenExpiryMinutes = 5;
    public const string EmbeddedSaltPlaceholder = "BCRYPT_EMBEDDED";
    public const int OtpMinValue = 100000;
    public const int OtpMaxValueExclusive = 999999;
    public const string UnknownFullNameFallback = "Unknown";
}

public static class ReauthClaims
{
    public const string TypeClaim = "TokenType";
    public const string ReauthValue = "Reauth";
}
