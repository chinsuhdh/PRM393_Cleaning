using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
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
            if (!result) return BadRequest("Email hoặc số điện thoại đã tồn tại trong hệ thống.");

            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var response = await _authService.LoginAsync(request, ipAddress, userAgent);
            if (response == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

            var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
            if (response == null) return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] string refreshToken)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.LogoutAsync(refreshToken, ipAddress);

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
    }
}