using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Account
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    public UserRole Role { get; set; } = UserRole.Client;

    public AccountStatus Status { get; set; } = AccountStatus.PendingVerification;

    public bool IsEmailVerified { get; set; }

    public bool IsPhoneVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AiConversation> AiConversations { get; set; } = new List<AiConversation>();

    public virtual ICollection<AiInferenceLog> AiInferenceLogs { get; set; } = new List<AiInferenceLog>();

    public virtual ICollection<BookingCancellation> BookingCancellations { get; set; } = new List<BookingCancellation>();

    public virtual ICollection<BookingRescheduleRequest> BookingRescheduleRequestResponders { get; set; } = new List<BookingRescheduleRequest>();

    public virtual ICollection<BookingRescheduleRequest> BookingRescheduleRequestRequesters { get; set; } = new List<BookingRescheduleRequest>();

    public virtual ICollection<BookingPhoto> BookingPhotos { get; set; } = new List<BookingPhoto>();

    public virtual ICollection<BookingStatusLog> BookingStatusLogs { get; set; } = new List<BookingStatusLog>();

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();
}
