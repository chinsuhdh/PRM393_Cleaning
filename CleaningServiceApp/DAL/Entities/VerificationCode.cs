
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class VerificationCode
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string CodeHash { get; set; } = null!;

    public VerificationPurpose Purpose { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}