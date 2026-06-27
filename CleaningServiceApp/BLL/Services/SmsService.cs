using Cleaning.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;

        public SmsService(ILogger<SmsService> logger)
        {
            _logger = logger;
        }

        public Task SendSmsAsync(string phoneNumber, string message)
        {
            // Tạm thời ghi log ra Console/File để test OTP
            _logger.LogInformation("SMS gửi tới {Phone}: {Message}", phoneNumber, message);
            return Task.CompletedTask;
        }
    }
}