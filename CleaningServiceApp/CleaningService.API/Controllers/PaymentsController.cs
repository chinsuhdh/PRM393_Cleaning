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

            try
            {
                var payment = await _paymentService.CreatePaymentAsync(request);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetPaymentByBooking(Guid bookingId)
        {
            var payment = await _paymentService.GetPaymentByBookingAsync(bookingId);

            if (payment == null)
                return NotFound(new { message = "Không tìm thấy thông tin thanh toán cho đơn này." });

            return Ok(payment);
        }

        // Thường API này sẽ được gọi bởi Webhook của MoMo/VNPay thay vì từ Flutter App
        [HttpPost("{id}/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(Guid id, [FromBody] PaymentCallbackDto request)
        {
            var result = await _paymentService.ProcessPaymentCallbackAsync(id, request);

            if (!result) return NotFound(new { message = "Không tìm thấy giao dịch hoặc lỗi hệ thống." });

            return Ok(new { message = "Đã cập nhật trạng thái thanh toán." });
        }
    }
}