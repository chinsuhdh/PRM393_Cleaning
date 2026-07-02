using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Common;
using Cleaning.BLL.Interfaces;
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

        [HttpPost("availability")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetAvailability([FromBody] BookingAvailabilityRequestDto request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await _bookingService.GetAvailabilityAsync(clientId, request));
            }
            catch (AppException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message, correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPost("quote")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetQuote([FromBody] BookingQuoteRequestDto request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await _bookingService.GetQuoteAsync(clientId, request));
            }
            catch (AppException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message, correlationId = HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (!Request.Headers.TryGetValue("Idempotency-Key", out var key))
                    throw new AppException(AppErrors.IdempotencyKeyRequired);
                var booking = await _bookingService.CreateBookingAsync(clientId, key.ToString(), request);

                return Ok(booking);
            }
            catch (AppException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message, correlationId = HttpContext.TraceIdentifier });
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

            if (!result) return NotFound(new { message = "Không tìm thấy đơn đặt lịch này hoặc có lỗi xảy ra." });

            return Ok(new { message = "Cập nhật trạng thái đơn thành công." });
        }

        [HttpGet("available")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetAvailableBookings()
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var bookings = await _bookingService.GetAvailableBookingsAsync(workerId);
            return Ok(bookings);
        }

        [HttpPatch("{id}/accept")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> AcceptBooking(Guid id)
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.AcceptBookingAsync(id, workerId);

            if (!result) return BadRequest(new { message = "Đơn hàng này không hợp lệ, hoặc đã có thợ khác nhanh tay nhận mất rồi." });

            return Ok(new { message = "Nhận đơn đặt lịch thành công!" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn đặt lịch này." });
            }

            return Ok(booking);
        }
    }
}
