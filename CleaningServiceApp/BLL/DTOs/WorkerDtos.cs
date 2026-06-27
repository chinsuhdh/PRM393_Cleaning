using System.ComponentModel.DataAnnotations;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.DTOs
{
    public class WorkerProfileDto
    {
        public Guid UserId { get; set; }
        public decimal AverageRating { get; set; }
        public string OnlineStatus { get; set; } = null!;
        public decimal? CurrentLat { get; set; }
        public decimal? CurrentLng { get; set; }
        public bool ImmediateBookingEnabled { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class RegisterWorkerProfileDto
    {
        public bool ImmediateBookingEnabled { get; set; }
    }

    public class UpdateLocationDto
    {
        [Required]
        public decimal CurrentLat { get; set; }

        [Required]
        public decimal CurrentLng { get; set; }
    }

    public class WorkerServiceDto
    {
        public Guid ServiceId { get; set; }
        public int ExperienceMonths { get; set; }
        public bool IsVerified { get; set; }
    }

    public class WorkerSkillDto : WorkerServiceDto
    {
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