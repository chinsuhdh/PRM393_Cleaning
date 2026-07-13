using System;
using System.ComponentModel.DataAnnotations;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.DTOs
{
    public class WorkerProfileDto
    {
        public Guid UserId { get; set; }
        public decimal AverageRating { get; set; }
        public WorkerOnlineStatus OnlineStatus { get; set; }
        public decimal? CurrentLat { get; set; }
        public decimal? CurrentLng { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public string? PayoutBankBin { get; set; }
        public string? PayoutBankAccountNumber { get; set; }
        public string? PayoutBankAccountName { get; set; }
    }

    public class UpdatePayoutAccountDto
    {
        [Required]
        public string BankBin { get; set; } = null!;

        [Required]
        public string AccountNumber { get; set; } = null!;

        [Required]
        public string AccountName { get; set; } = null!;
    }

    public class WorkerEarningDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime EarnedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PayoutFailureReason { get; set; }
    }

    public class RegisterWorkerProfileDto
    {
        [Required]
        public decimal CurrentLat { get; set; }

        [Required]
        public decimal CurrentLng { get; set; }
    }

    public class UpdateLocationDto
    {
        [Required]
        public decimal CurrentLat { get; set; }

        [Required]
        public decimal CurrentLng { get; set; }
    }

    public class UpdateOnlineStatusDto
    {
        [Required]
        public WorkerOnlineStatus OnlineStatus { get; set; }
    }

    public class WorkerSkillDto
    {
        [Required]
        public Guid ServiceId { get; set; }

        [Required]
        [Range(0, 600, ErrorMessage = "Experience months must be a valid number.")]
        public int ExperienceMonths { get; set; }

        public bool IsVerified { get; set; }
    }

    public class WorkerDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Initials { get; set; } = null!;
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public string Distance { get; set; } = "Unknown";
        public int MatchPercentage { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
