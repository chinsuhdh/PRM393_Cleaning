using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            if (!result) return BadRequest(new { message = "Email hoặc số điện thoại đã tồn tại, hoặc có lỗi hệ thống xảy ra." });

            return Ok(new { message = "Đăng ký thành công! Vui lòng kiểm tra email để nhận mã OTP." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                var response = await _authService.LoginAsync(request, ipAddress, userAgent);
                if (response == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto request)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

            var response = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            if (response == null) return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] TokenRequestDto request)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.LogoutAsync(request.RefreshToken, ipAddress);

            return Ok(new { message = "Đã đăng xuất khỏi thiết bị." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result) return NotFound(new { message = "Không tìm thấy email trong hệ thống." });

            return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(request);
            if (!result) return BadRequest(new { message = "OTP sai, đã hết hạn hoặc đã được sử dụng." });

            return Ok(new { message = "Đặt lại mật khẩu thành công!" });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.VerifyAccountAsync(request);
            if (!result) return BadRequest(new { message = "Mã OTP không hợp lệ, đã hết hạn hoặc tài khoản không tồn tại." });

            return Ok(new { message = "Xác thực tài khoản thành công! Bạn có thể đăng nhập." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var result = await _authService.ChangePasswordAsync(userId, request);
            if (!result) return BadRequest(new { message = "Mật khẩu cũ không đúng hoặc tài khoản không tồn tại." });

            return Ok(new { message = "Thay đổi mật khẩu thành công!" });
        }

        [HttpPost("send-phone-otp")]
        [Authorize]
        public async Task<IActionResult> SendPhoneOtp()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var result = await _authService.SendPhoneVerificationOtpAsync(userId);
            if (!result) return BadRequest(new { message = "Không thể gửi OTP. Hãy kiểm tra lại số điện thoại trong hồ sơ." });

            return Ok(new { message = "Mã xác thực SMS đã được gửi." });
        }

        [HttpPost("verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.VerifyPhoneAsync(request);
            if (!result) return BadRequest(new { message = "Mã OTP sai, hết hạn hoặc số điện thoại không tồn tại." });

            return Ok(new { message = "Xác thực số điện thoại thành công!" });
        }
    }
}