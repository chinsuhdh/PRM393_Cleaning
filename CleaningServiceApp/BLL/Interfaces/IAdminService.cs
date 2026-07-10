using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync();

        // Worker Applications
        Task<IEnumerable<WorkerApplicationDto>> GetWorkerApplicationsAsync();
        Task<bool> ApproveWorkerApplicationAsync(Guid applicationId, ApproveWorkerApplicationDto dto);
        Task<bool> RejectWorkerApplicationAsync(Guid applicationId, RejectWorkerApplicationDto dto);

        // Account Management
        Task<IEnumerable<AccountAdminDto>> GetAccountsAsync();
        Task<bool> ChangeAccountStatusAsync(Guid accountId, UpdateAccountStatusDto dto);

        // Service Management
        Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto);
        Task<ServiceDto?> UpdateServiceAsync(Guid serviceId, UpdateServiceDto dto);
        Task<bool> ArchiveServiceAsync(Guid serviceId);

        // Booking Management
        Task<IEnumerable<BookingAdminDto>> GetAllBookingsAsync();
    }
}