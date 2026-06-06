using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var booking = await _bookingService.CreateBookingAsync(clientId, request);

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("client")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetMyClientBookings()
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var bookings = await _bookingService.GetClientBookingsAsync(clientId);

            return Ok(bookings);
        }

        [HttpGet("worker")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetMyWorkerBookings()
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var bookings = await _bookingService.GetWorkerBookingsAsync(workerId);

            return Ok(bookings);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] UpdateBookingStatusDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.UpdateBookingStatusAsync(id, accountId, request);

            if (!result) return NotFound(new { message = "Booking not found." });

            return Ok(new { message = "Status updated successfully." });
        }

        [HttpGet("available")]
        [Authorize(Roles = "Worker")] // Chỉ thợ mới được xem danh sách đơn trống
        public async Task<IActionResult> GetAvailableBookings()
        {
            var bookings = await _bookingService.GetAvailableBookingsAsync();
            return Ok(bookings);
        }

        [HttpPatch("{id}/accept")]
        [Authorize(Roles = "Worker")] // Chỉ thợ mới được nhận đơn
        public async Task<IActionResult> AcceptBooking(Guid id)
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.AcceptBookingAsync(id, workerId);

            if (!result) return BadRequest(new { message = "Đơn hàng này không hợp lệ, hoặc đã có thợ khác nhanh tay nhận mất rồi." });

            return Ok(new { message = "Nhận đơn đặt lịch thành công!" });
        }
    }
}