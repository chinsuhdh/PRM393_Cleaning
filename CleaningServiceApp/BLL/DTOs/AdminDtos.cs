namespace Cleaning.BLL.DTOs
{
    public class AdminDashboardStatsDto
    {
        public int TotalClients { get; set; }
        public int TotalWorkers { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}