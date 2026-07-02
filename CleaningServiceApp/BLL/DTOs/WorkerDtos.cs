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
        public bool ImmediateBookingEnabled { get; set; }
        public DateTime? VerifiedAt { get; set; }
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
    }
}