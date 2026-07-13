using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IWorkerService
    {
        Task<WorkerProfileDto?> GetWorkerProfileAsync(Guid workerId);
        Task<bool> RegisterWorkerProfileAsync(Guid workerId, RegisterWorkerProfileDto request);
        Task<bool> UpdateLocationAsync(Guid workerId, UpdateLocationDto request);
        Task<bool> UpdateOnlineStatusAsync(Guid workerId, UpdateOnlineStatusDto request);
        Task<IEnumerable<WorkerSkillDto>> GetWorkerSkillsAsync(Guid workerId);
        Task<bool> AddOrUpdateWorkerSkillAsync(Guid workerId, WorkerSkillDto request);
        Task<bool> UpdatePayoutAccountAsync(Guid workerId, UpdatePayoutAccountDto request);
        Task<IEnumerable<WorkerEarningDto>> GetWorkerEarningsAsync(Guid workerId);
    }
}