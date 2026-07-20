using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync();

        Task<IEnumerable<WorkerApplicationDto>> GetWorkerApplicationsAsync();
        Task<bool> ApproveWorkerApplicationAsync(Guid applicationId, ApproveWorkerApplicationDto dto);
        Task<bool> RejectWorkerApplicationAsync(Guid applicationId, RejectWorkerApplicationDto dto);

        Task<IEnumerable<AccountAdminDto>> GetAccountsAsync();
        Task<bool> ChangeAccountStatusAsync(Guid accountId, UpdateAccountStatusDto dto);

        Task<IEnumerable<ServiceDto>> GetAllServicesAsync();
        Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto);
        Task<ServiceDto?> UpdateServiceAsync(Guid serviceId, UpdateServiceDto dto);
        Task<bool> ArchiveServiceAsync(Guid serviceId);

        Task<IEnumerable<BookingAdminDto>> GetAllBookingsAsync();
    }
}