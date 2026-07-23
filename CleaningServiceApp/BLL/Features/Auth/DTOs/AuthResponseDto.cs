namespace Cleaning.BLL.Features.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Guid ProfileId { get; set; }
        public string FullName { get; set; } = null!;
    }

    public class TokenRequestDto
    {
        public string RefreshToken { get; set; } = null!;
    }
}