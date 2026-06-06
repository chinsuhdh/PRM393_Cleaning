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
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var payment = await _paymentService.CreatePaymentAsync(request);
            return Ok(payment);
        }

        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetPaymentsByBooking(Guid bookingId)
        {
            var payments = await _paymentService.GetPaymentsByBookingAsync(bookingId);
            return Ok(payments);
        }

        // Thường API này sẽ được gọi bởi Webhook của MoMo/VNPay thay vì từ Flutter App
        [HttpPost("{id}/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(Guid id, [FromBody] PaymentCallbackDto request)
        {
            var result = await _paymentService.ProcessPaymentCallbackAsync(id, request);
            if (!result) return NotFound(new { message = "Payment not found." });

            return Ok(new { message = "Payment processed." });
        }
    }
}