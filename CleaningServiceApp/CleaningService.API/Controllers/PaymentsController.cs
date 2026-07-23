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

        [HttpGet("vnpay-confirm")]
        public async Task<IActionResult> VnpayConfirm()
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var outcome = await _paymentService.ConfirmVnpayPaymentAsync(queryParams);

            var success = outcome is VnpayConfirmOutcome.Success or VnpayConfirmOutcome.OrderAlreadyConfirmed;
            return Ok(new { success, outcome = outcome.ToString() });
        }
    }
}

// Payment processing documentation added
