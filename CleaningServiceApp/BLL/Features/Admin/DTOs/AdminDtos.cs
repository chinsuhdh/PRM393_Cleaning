namespace Cleaning.BLL.Features.Admin
{
    public class AdminDashboardStatsDto
    {
        public int TotalClients { get; set; }
        public int TotalWorkers { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class WorkerApplicationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = null!;
        public string? GovernmentId { get; set; }
        public string? ExperienceSummary { get; set; }
        public string Evidence { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class ApproveWorkerApplicationDto
    {
        public Guid AdminId { get; set; }
    }

    public class RejectWorkerApplicationDto
    {
        public Guid AdminId { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class AccountAdminDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateAccountStatusDto
    {
        public string Status { get; set; } = null!;
    }

    public class CreateServiceDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string PropertyType { get; set; } = null!;
        public string UnitType { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int MinimumHours { get; set; }
        public string BookingFormSchema { get; set; } = "{}";
        public string OperatingSchedule { get; set; } = "{}";
    }

    public class UpdateServiceDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int MinimumHours { get; set; }
        public string BookingFormSchema { get; set; } = "{}";
        public string OperatingSchedule { get; set; } = "{}";
        public bool IsActive { get; set; }
    }

    public class BookingAdminDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Guid? WorkerId { get; set; }
        public Guid ServiceId { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalPrice { get; set; }
        public DateTime ScheduledStartTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}