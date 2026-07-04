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
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _bookingService.GetAvailabilityAsync(clientId, request));
        }

        [HttpPost("quote")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetQuote([FromBody] BookingQuoteRequestDto request)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _bookingService.GetQuoteAsync(clientId, request));
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto request)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!Request.Headers.TryGetValue("Idempotency-Key", out var key))
                throw new AppException(AppErrors.IdempotencyKeyRequired);

            var booking = await _bookingService.CreateBookingAsync(clientId, key.ToString(), request);
            return Ok(booking);
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
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.UpdateBookingStatusAsync(id, accountId, request);

            if (!result) throw new AppException(AppErrors.BookingStatusUpdateFailed);

            return Ok(ApiResponse.Message(ResponseMessages.BookingStatusUpdated));
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

            if (!result) throw new AppException(AppErrors.BookingAcceptFailed);

            return Ok(ApiResponse.Message(ResponseMessages.BookingAccepted));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null) throw new AppException(AppErrors.BookingNotFound);

            return Ok(booking);
        }
    }
}
