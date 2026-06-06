namespace Cleaning.BLL.DTOs
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Guid ProfileId { get; set; }
        public string FullName { get; set; } = null!;
    }
}