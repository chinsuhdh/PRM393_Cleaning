using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs
{
    public class WorkerProfileDto
    {
        public Guid UserId { get; set; }
        public string? IdentityCardNumber { get; set; }
        public decimal AverageRating { get; set; }
        public int CompletedJobs { get; set; }
        public decimal? CurrentLat { get; set; }
        public decimal? CurrentLng { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class RegisterWorkerProfileDto
    {
        [Required]
        public string IdentityCardNumber { get; set; } = null!;
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
        public Guid ServiceId { get; set; }
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
