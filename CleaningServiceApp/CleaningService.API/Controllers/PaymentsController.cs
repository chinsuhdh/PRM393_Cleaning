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
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request)
        {
            var payment = await _paymentService.CreatePaymentAsync(request);
            return Ok(payment);
        }

        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetPaymentByBooking(Guid bookingId)
        {
            var payment = await _paymentService.GetPaymentByBookingAsync(bookingId);

            if (payment == null) throw new AppException(AppErrors.PaymentNotFound);

            return Ok(payment);
        }

        [HttpGet("vnpay-account")]
        public async Task<IActionResult> GetVnpayAccount()
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _paymentService.GetVnpayAccountAsync(accountId));
        }

        [HttpPut("vnpay-account")]
        public async Task<IActionResult> LinkVnpayAccount([FromBody] VnpayAccountDto request)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _paymentService.LinkVnpayAccountAsync(accountId, request));
        }

        // Thường API này sẽ được gọi bởi Webhook của MoMo/VNPay thay vì từ Flutter App
        [HttpPost("{id}/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(Guid id, [FromBody] PaymentCallbackDto request)
        {
            var result = await _paymentService.ProcessPaymentCallbackAsync(id, request);

            if (!result) throw new AppException(AppErrors.PaymentCallbackFailed);

            return Ok(ApiResponse.Message(ResponseMessages.PaymentCallbackProcessed));
        }
    }
}
