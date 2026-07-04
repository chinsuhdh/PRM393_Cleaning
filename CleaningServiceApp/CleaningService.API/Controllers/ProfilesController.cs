using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using CleaningService.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IWebHostEnvironment _environment;

        public ProfilesController(IProfileService profileService, IWebHostEnvironment environment)
        {
            _profileService = profileService;
            _environment = environment;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var profile = await _profileService.GetProfileAsync(userId);
            if (profile == null)
                throw new AppException(AppErrors.ProfileNotFound);

            return Ok(profile);
        }

        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _profileService.UpdateProfileAsync(userId, request);

            if (!result)
                throw new AppException(AppErrors.ProfileUpdateFailed);

            return Ok(ApiResponse.Message(ResponseMessages.ProfileUpdated));
        }

        [HttpPost("me/avatar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
                // [FIX LỖI PATH1 NULL]: Lấy đường dẫn wwwroot, nếu chưa có thì tự động tạo từ thư mục gốc
                string webRootPath = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                string folderPath = Path.Combine(webRootPath, "uploads", "avatars");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string uniqueFileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";
                string filePath = Path.Combine(folderPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var request = HttpContext.Request;
                string avatarUrl = $"{request.Scheme}://{request.Host}/uploads/avatars/{uniqueFileName}";

                await _profileService.UpdateAvatarAsync(userId, avatarUrl);

                return Ok(ApiResponse.Ok(new { avatarUrl }, ResponseMessages.AvatarUploaded));
            }
            catch (Exception)
            {
                throw new AppException(AppErrors.AvatarUploadFailed);
            }
        }
    }
}
