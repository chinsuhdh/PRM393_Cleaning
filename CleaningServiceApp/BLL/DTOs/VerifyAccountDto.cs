namespace Cleaning.BLL.DTOs
{
    public class VerifyAccountDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
    }
}