using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using CleaningService.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IFileStorageService _fileStorage;

        public ProfilesController(IProfileService profileService, IFileStorageService fileStorage, IConfiguration configuration)
        {
            _profileService = profileService;
            _fileStorage = fileStorage;
            _configuration = configuration;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var profile = await _profileService.GetProfileAsync(userId);
            if (profile == null) throw new AppException(AppErrors.ProfileNotFound);

            return Ok(profile);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfileById(Guid id)
        {
            var profile = await _profileService.GetProfileAsync(id);
            if (profile == null) throw new AppException(AppErrors.ProfileNotFound);

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _profileService.UpdateProfileAsync(userId, request);
            if (!result) throw new AppException(AppErrors.ProfileUpdateFailed);

            return Ok(new
            {
                success = true,
                message = ResponseMessages.ProfileUpdated
            });
        }

        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new AppException(AppErrors.AvatarFileRequired);

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                throw new AppException(AppErrors.AvatarFileTypeInvalid);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            try
            {
                string uniqueFileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";

                await using var stream = file.OpenReadStream();
                string avatarUrl = await _fileStorage.UploadAsync(stream, uniqueFileName, "avatars");

                await _profileService.UpdateAvatarAsync(userId, avatarUrl);

                return Ok(ApiResponse.Ok(new { avatarUrl }, ResponseMessages.AvatarUploaded));
            }
            catch (Exception)
            {
                throw new AppException(AppErrors.AvatarUploadFailed);
            }
        }

        [HttpPost("me/onboarding")]
        public async Task<IActionResult> CompleteOnboarding()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _profileService.CompleteOnboardingAsync(userId);
            if (!result) throw new AppException(AppErrors.ProfileUpdateFailed);

            return Ok(ApiResponse.Message("Onboarding completed successfully."));
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount([FromHeader(Name = "X-Reauth-Token")] string reauthToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            if (string.IsNullOrWhiteSpace(reauthToken))
                throw new AppException(AppErrors.Unauthorized);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtConfig:Secret"]!);
            try
            {
                tokenHandler.ValidateToken(reauthToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtConfig:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtConfig:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var tokenType = jwtToken.Claims.FirstOrDefault(x => x.Type == "TokenType")?.Value;
                var tokenUserId = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;

                if (tokenType != "Reauth" || tokenUserId != userId.ToString())
                {
                    throw new AppException(AppErrors.Unauthorized);
                }
            }
            catch
            {
                throw new AppException(AppErrors.Unauthorized);
            }

            var result = await _profileService.DeleteAccountAsync(userId);
            if (!result) throw new AppException(AppErrors.ProfileUpdateFailed);

            return Ok(ApiResponse.Message("Tài khoản đã được xóa vĩnh viễn."));
        }
    }
}