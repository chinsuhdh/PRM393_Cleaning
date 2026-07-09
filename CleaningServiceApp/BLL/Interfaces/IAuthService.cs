using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent);
        Task<bool> RegisterAsync(RegisterRequestDto request);
        Task<bool> LogoutAsync(string refreshToken, string? ipAddress);
        Task<AuthResponseDto?> RefreshTokenAsync(string oldRefreshToken, string? ipAddress);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<bool> ResetPasswordAsync(ResetPasswordDto request);
        Task<bool> VerifyAccountAsync(VerifyAccountDto request);


        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request);
        Task<bool> VerifyPhoneAsync(VerifyPhoneDto request);
        Task<bool> SendPhoneVerificationOtpAsync(Guid userId);


        Task<string?> ReauthenticateAsync(Guid userId, string password);
    }
}