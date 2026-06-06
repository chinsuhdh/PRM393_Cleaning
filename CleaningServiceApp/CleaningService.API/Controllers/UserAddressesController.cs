using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetMyAddresses()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var addresses = await _userAddressService.GetUserAddressesAsync(userId);
            return Ok(addresses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddress(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var address = await _userAddressService.GetAddressByIdAsync(id, userId);

            if (address == null) return NotFound();
            return Ok(address);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateUserAddressDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var newAddress = await _userAddressService.CreateAddressAsync(userId, request);

            return CreatedAtAction(nameof(GetAddress), new { id = newAddress.Id }, newAddress);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateUserAddressDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _userAddressService.UpdateAddressAsync(id, userId, request);

            if (!result) return NotFound();
            return Ok(new { message = "Address updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _userAddressService.DeleteAddressAsync(id, userId);

            if (!result) return NotFound();
            return Ok(new { message = "Address deleted successfully." });
        }

        [HttpPatch("{id}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _userAddressService.SetDefaultAddressAsync(id, userId);

            if (!result) return NotFound();
            return Ok(new { message = "Default address updated successfully." });
        }
    }
}