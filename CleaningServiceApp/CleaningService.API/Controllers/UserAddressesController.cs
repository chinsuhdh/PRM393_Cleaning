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
    [Authorize(Roles = "Client")]
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
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var addresses = await _userAddressService.GetUserAddressesAsync(userId);
            return Ok(addresses);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserAddressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAddress(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var address = await _userAddressService.GetAddressByIdAsync(id, userId);

            if (address == null) throw new AppException(AppErrors.AddressNotFound);
            return Ok(address);
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserAddressDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAddress([FromBody] CreateUserAddressDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var newAddress = await _userAddressService.CreateAddressAsync(userId, request);

            return CreatedAtAction(nameof(GetAddress), new { id = newAddress.Id }, newAddress);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateUserAddressDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _userAddressService.UpdateAddressAsync(id, userId, request);

            if (!result) throw new AppException(AppErrors.AddressNotFound);
            return Ok(ApiResponse.Message(ResponseMessages.AddressUpdated));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _userAddressService.DeleteAddressAsync(id, userId);

            if (!result) throw new AppException(AppErrors.AddressNotFound);
            return Ok(ApiResponse.Message(ResponseMessages.AddressDeleted));
        }

        [HttpPatch("{id}/set-default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new AppException(AppErrors.Unauthorized);

            var result = await _userAddressService.SetDefaultAddressAsync(id, userId);

            if (!result) throw new AppException(AppErrors.AddressNotFound);
            return Ok(ApiResponse.Message(ResponseMessages.DefaultAddressUpdated));
        }
    }
}
