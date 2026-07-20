using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
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
        private readonly IFileStorageService _fileStorage;

        public BookingsController(IBookingService bookingService, IFileStorageService fileStorage)
        {
            _bookingService = bookingService;
            _fileStorage = fileStorage;
        }

        [HttpPost("{id}/photos")]
        [Authorize(Roles = "Client")]
        [RequestSizeLimit(BookingDomainConstants.MaxPhotoRequestBytes)]
        public async Task<IActionResult> UploadPhotos(Guid id, [FromForm] List<IFormFile> photos)
        {
            if (photos.Count is < 1 or > BookingDomainConstants.MaxPhotosPerBooking || photos.Any(photo =>
                    photo.Length > BookingDomainConstants.MaxPhotoBytes || !photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
                return BadRequest(ApiResponse.Message("Tối đa 5 ảnh, mỗi ảnh không quá 1 MB."));

            var urls = new List<string>();
            foreach (var photo in photos)
            {
                var extension = Path.GetExtension(photo.FileName);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                await using var stream = photo.OpenReadStream();
                urls.Add(await _fileStorage.UploadAsync(stream, fileName, "bookings"));
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

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CancelByClient(Guid id)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.CancelByClientAsync(id, clientId);

            if (!result) throw new AppException(AppErrors.BookingCancelNotAllowed);

            return Ok(ApiResponse.Message(ResponseMessages.BookingCancelled));
        }

        [HttpPost("{id}/worker-cancel")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> WorkerCancel(Guid id, [FromBody] WorkerCancelBookingDto request)
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _bookingService.WorkerCancelAsync(id, workerId, request);

            return Ok(ApiResponse.Message(ResponseMessages.BookingWorkerCancelled));
        }

        [HttpPost("{id}/switch-to-cash")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SwitchToCash(Guid id)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _bookingService.SwitchToCashAsync(id, clientId);

            return Ok(ApiResponse.Message(ResponseMessages.BookingSwitchedToCash));
        }

        [HttpPost("{id}/client-cancel")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> ClientCancel(Guid id, [FromBody] ClientCancelBookingDto request)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _bookingService.ClientCancelAsync(id, clientId, request);

            return Ok(ApiResponse.Message(ResponseMessages.BookingClientCancelled));
        }

        [HttpPost("{id}/report")]
        public async Task<IActionResult> ReportBooking(Guid id, [FromBody] ReportBookingDto request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _bookingService.ReportBookingAsync(id, accountId, request);

            return Ok(ApiResponse.Message(ResponseMessages.BookingReported));
        }

        [HttpPost("{id}/reschedule")]
        public async Task<IActionResult> ProposeReschedule(Guid id, [FromBody] ProposeRescheduleDto request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var booking = await _bookingService.ProposeRescheduleAsync(id, accountId, request);
            return Ok(booking);
        }

        [HttpPatch("{id}/reschedule/{reqId}")]
        public async Task<IActionResult> RespondReschedule(Guid id, Guid reqId, [FromBody] RespondRescheduleDto request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var booking = await _bookingService.RespondRescheduleAsync(id, reqId, accountId, request.Action);
            return Ok(booking);
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
            return Ok(ApiResponse.Message(ResponseMessages.BroadcastRestarted));
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
                ? Ok(ApiResponse.Message(ResponseMessages.JobHidden))
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
