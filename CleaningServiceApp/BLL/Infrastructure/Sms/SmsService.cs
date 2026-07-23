using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Infrastructure.Sms
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
            _logger.LogInformation("SMS gửi tới {Phone}: {Message}", phoneNumber, message);
            return Task.CompletedTask;
        }
    }
}