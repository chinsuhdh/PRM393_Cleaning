using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using CleaningService.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result) throw new AppException(AppErrors.AuthRegisterFailed);

            return Ok(ApiResponse.Message(ResponseMessages.RegisterSuccess));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var response = await _authService.LoginAsync(request, ipAddress, userAgent);
            if (response == null) throw new AppException(AppErrors.AuthInvalidCredentials);

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto request)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

            var response = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            if (response == null) throw new AppException(AppErrors.AuthInvalidRefreshToken);

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] TokenRequestDto request)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.LogoutAsync(request.RefreshToken, ipAddress);

            return Ok(ApiResponse.Message(ResponseMessages.LogoutSuccess));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result) throw new AppException(AppErrors.AuthEmailNotFound);

            return Ok(ApiResponse.Message(ResponseMessages.ForgotPasswordOtpSent));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result) throw new AppException(AppErrors.AuthResetPasswordFailed);

            return Ok(ApiResponse.Message(ResponseMessages.ResetPasswordSuccess));
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountDto request)
        {
            var result = await _authService.VerifyAccountAsync(request);
            if (!result) throw new AppException(AppErrors.AuthVerifyAccountFailed);

            return Ok(ApiResponse.Message(ResponseMessages.VerifyAccountSuccess));
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _authService.ChangePasswordAsync(userId, request);
            if (!result) throw new AppException(AppErrors.AuthChangePasswordFailed);

            return Ok(ApiResponse.Message(ResponseMessages.ChangePasswordSuccess));
        }

        [HttpPost("send-phone-otp")]
        [Authorize]
        public async Task<IActionResult> SendPhoneOtp()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _authService.SendPhoneVerificationOtpAsync(userId);
            if (!result) throw new AppException(AppErrors.AuthSendPhoneOtpFailed);

            return Ok(ApiResponse.Message(ResponseMessages.PhoneOtpSent));
        }

        [HttpPost("verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneDto request)
        {
            var result = await _authService.VerifyPhoneAsync(request);
            if (!result) throw new AppException(AppErrors.AuthVerifyPhoneFailed);

            return Ok(ApiResponse.Message(ResponseMessages.VerifyPhoneSuccess));
        }
    }
}
