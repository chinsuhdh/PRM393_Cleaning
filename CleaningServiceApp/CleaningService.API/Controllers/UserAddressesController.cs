using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Client")] // Nếu Admin cũng có quyền quản lý, có thể sửa thành "Client,Admin"
    public class UserAddressesController : ControllerBase
    {
        private readonly IUserAddressService _userAddressService;

        public UserAddressesController(IUserAddressService userAddressService)
        {
            _userAddressService = userAddressService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserAddressDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAddresses()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var addresses = await _userAddressService.GetUserAddressesAsync(userId);
            return Ok(addresses);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserAddressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAddress(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var address = await _userAddressService.GetAddressByIdAsync(id, userId);

            if (address == null) return NotFound(new { message = "Address not found." });
            return Ok(address);
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserAddressDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAddress([FromBody] CreateUserAddressDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var newAddress = await _userAddressService.CreateAddressAsync(userId, request);

            return CreatedAtAction(nameof(GetAddress), new { id = newAddress.Id }, newAddress);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateUserAddressDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var result = await _userAddressService.UpdateAddressAsync(id, userId, request);

            if (!result) return NotFound(new { message = "Address not found." });
            return Ok(new { message = "Address updated successfully." });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var result = await _userAddressService.DeleteAddressAsync(id, userId);

            if (!result) return NotFound(new { message = "Address not found." });
            return Ok(new { message = "Address deleted successfully." });
        }

        [HttpPatch("{id}/set-default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var result = await _userAddressService.SetDefaultAddressAsync(id, userId);

            if (!result) return NotFound(new { message = "Address not found." });
            return Ok(new { message = "Default address updated successfully." });
        }
    }
}