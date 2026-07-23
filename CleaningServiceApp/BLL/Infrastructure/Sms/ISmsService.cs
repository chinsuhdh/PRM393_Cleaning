namespace Cleaning.BLL.Infrastructure.Sms
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}