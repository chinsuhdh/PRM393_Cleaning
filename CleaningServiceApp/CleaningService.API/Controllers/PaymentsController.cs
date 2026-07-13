using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> PayNow([FromBody] PayNowRequestDto request)
        {
            var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _paymentService.PayNowAsync(clientId, request, ipAddress);
            return Ok(result);
        }

        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetPaymentByBooking(Guid bookingId)
        {
            var payment = await _paymentService.GetPaymentByBookingAsync(bookingId);

            if (payment == null) throw new AppException(AppErrors.PaymentNotFound);

            return Ok(payment);
        }

        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var rawJson = await reader.ReadToEndAsync();
            var success = await _paymentService.ProcessPayOsWebhookAsync(rawJson);
            return success ? Ok() : BadRequest();
        }

        [HttpGet("payos-return")]
        [AllowAnonymous]
        public IActionResult PayOsReturn() =>
            Content("Bạn có thể đóng cửa sổ này.", "text/plain");
    }
}
