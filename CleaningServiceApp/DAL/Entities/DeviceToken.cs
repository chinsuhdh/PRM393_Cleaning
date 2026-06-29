namespace Cleaning.DAL.Entities;

public class DeviceToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
