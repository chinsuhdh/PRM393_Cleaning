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
    }
}