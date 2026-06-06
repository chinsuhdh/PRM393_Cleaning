using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Account
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool IsPhoneVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AiConversation> AiConversations { get; set; } = new List<AiConversation>();

    public virtual ICollection<AiInferenceLog> AiInferenceLogs { get; set; } = new List<AiInferenceLog>();

    public virtual ICollection<BookingStatusLog> BookingStatusLogs { get; set; } = new List<BookingStatusLog>();

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public UserRole Role { get; set; } = UserRole.Client;
    public AccountStatus Status { get; set; } = AccountStatus.PendingVerification;
}
