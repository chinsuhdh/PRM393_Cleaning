using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Enums;
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
        private readonly IWebHostEnvironment _environment;

        public BookingsController(IBookingService bookingService, IWebHostEnvironment environment)
        {
            _bookingService = bookingService;
            _environment = environment;
        }

        [HttpPost("{id}/photos")]
        [Authorize(Roles = "Client")]
        [RequestSizeLimit(5_242_880)]
        public async Task<IActionResult> UploadPhotos(Guid id, [FromForm] List<IFormFile> photos)
        {
            if (photos.Count is < 1 or > 5 || photos.Any(photo =>
                    photo.Length > 1_048_576 || !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
                return BadRequest(ApiResponse.Message("Tối đa 5 ảnh, mỗi ảnh không quá 1 MB."));

            var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "bookings");
            Directory.CreateDirectory(folder);
            var urls = new List<string>();
            foreach (var photo in photos)
            {
                var extension = Path.GetExtension(photo.FileName);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
                await photo.CopyToAsync(stream);
                urls.Add($"{Request.Scheme}://{Request.Host}/uploads/bookings/{fileName}");
            }
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.AddPhotosAsync(id, accountId, urls);
            return result == null ? Forbid() : Ok(result);
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
            await _bookingService.BroadcastBookingAsync(booking.Id);
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

        [HttpPost("{id}/retry")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> RetryBroadcast(Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var booking = await _bookingService.GetBookingByIdAsync(id, accountId);
            if (booking == null || booking.Status != nameof(BookingStatus.AwaitingWorker))
                throw new AppException(AppErrors.BookingNotFound);
            await _bookingService.BroadcastBookingAsync(id);
            return Ok(ApiResponse.Message("Broadcast restarted."));
        }

        [HttpGet("{id}/nearby-workers")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetNearbyWorkers(Guid id)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var locations = await _bookingService.GetNearbyOnlineWorkerLocationsAsync(id, clientId);
            return Ok(locations);
        }

        [HttpPost("{id}/hide")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> HideBooking(Guid id)
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _bookingService.HideBookingAsync(id, workerId)
                ? Ok(ApiResponse.Message("Job hidden."))
                : NotFound();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.IsInRole("Admin");
            var booking = isAdmin
                ? await _bookingService.GetBookingByIdAsync(id)
                : await _bookingService.GetBookingByIdAsync(id, accountId);

            if (booking == null) throw new AppException(AppErrors.BookingNotFound);

            return Ok(booking);
        }
    }
}
