using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using WorkerServiceEntity = Cleaning.DAL.Entities.WorkerService;

namespace Cleaning.BLL.Services
{
    public class WorkerService : IWorkerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WorkerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<WorkerProfileDto?> GetWorkerProfileAsync(Guid workerId)
        {
            var worker = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
            if (worker == null) return null;

            return new WorkerProfileDto
            {
                UserId = worker.UserId,
                AverageRating = worker.AverageRating,
                OnlineStatus = worker.OnlineStatus.ToString(),
                CurrentLat = worker.CurrentLat,
                CurrentLng = worker.CurrentLng,
                ImmediateBookingEnabled = worker.ImmediateBookingEnabled,
                VerifiedAt = worker.VerifiedAt
            };
        }

        public async Task<bool> RegisterWorkerProfileAsync(Guid workerId, RegisterWorkerProfileDto request)
        {
            var exists = await _unitOfWork.Repository<WorkerProfile>().ExistsAsync(w => w.UserId == workerId);
            if (exists) return false;

            var newWorker = new WorkerProfile
            {
                UserId = workerId,
                AverageRating = 5.0m,
                OnlineStatus = WorkerOnlineStatus.Offline,
                ImmediateBookingEnabled = request.ImmediateBookingEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<WorkerProfile>().AddAsync(newWorker);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateLocationAsync(Guid workerId, UpdateLocationDto request)
        {
            var worker = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
            if (worker == null) return false;

            worker.CurrentLat = request.CurrentLat;
            worker.CurrentLng = request.CurrentLng;
            worker.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<WorkerProfile>().Update(worker);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<WorkerSkillDto>> GetWorkerSkillsAsync(Guid workerId)
        {
            var services = await _unitOfWork.Repository<WorkerServiceEntity>().FindAsync(ws => ws.WorkerId == workerId);

            return services.Select(ws => new WorkerSkillDto
            {
                ServiceId = ws.ServiceId,
                ExperienceMonths = ws.ExperienceMonths,
                IsVerified = ws.IsVerified
            });
        }

        public async Task<bool> AddOrUpdateWorkerSkillAsync(Guid workerId, WorkerSkillDto request)
        {
            var workerService = await _unitOfWork.Repository<WorkerServiceEntity>()
                .FirstOrDefaultAsync(ws => ws.WorkerId == workerId && ws.ServiceId == request.ServiceId);

            if (workerService != null)
            {
                workerService.ExperienceMonths = request.ExperienceMonths;
                _unitOfWork.Repository<WorkerServiceEntity>().Update(workerService);
            }
            else
            {
                var newWorkerService = new WorkerServiceEntity
                {
                    WorkerId = workerId,
                    ServiceId = request.ServiceId,
                    ExperienceMonths = request.ExperienceMonths,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<WorkerServiceEntity>().AddAsync(newWorkerService);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
