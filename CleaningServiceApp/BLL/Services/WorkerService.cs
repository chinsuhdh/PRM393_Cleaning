using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Cleaning.DAL.Enums;

// Thêm dòng Alias này để phân biệt Entity trong DB với Class Service hiện tại
using DalWorkerService = Cleaning.DAL.Entities.WorkerService;

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
                OnlineStatus = worker.OnlineStatus,
                CurrentLat = worker.CurrentLat,
                CurrentLng = worker.CurrentLng,
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
                CurrentLat = request.CurrentLat,
                CurrentLng = request.CurrentLng,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LocationUpdatedAt = DateTime.UtcNow
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
            worker.LocationUpdatedAt = DateTime.UtcNow;
            worker.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<WorkerProfile>().Update(worker);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<WorkerSkillDto>> GetWorkerSkillsAsync(Guid workerId)
        {
            // Sử dụng DalWorkerService thay vì WorkerService
            var skills = await _unitOfWork.Repository<DalWorkerService>()
                .FindAsync(ws => ws.WorkerId == workerId);

            return skills.Select(ws => new WorkerSkillDto
            {
                ServiceId = ws.ServiceId,
                ExperienceMonths = ws.ExperienceMonths,
                IsVerified = ws.IsVerified
            }).ToList();
        }

        public async Task<bool> AddOrUpdateWorkerSkillAsync(Guid workerId, WorkerSkillDto request)
        {
            // Sử dụng DalWorkerService thay vì WorkerService
            var skill = await _unitOfWork.Repository<DalWorkerService>()
                .FirstOrDefaultAsync(ws => ws.WorkerId == workerId && ws.ServiceId == request.ServiceId);

            if (skill != null)
            {
                skill.ExperienceMonths = request.ExperienceMonths;
                skill.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<DalWorkerService>().Update(skill);
            }
            else
            {
                // Khởi tạo instance của Entity bằng Alias
                var newSkill = new DalWorkerService
                {
                    WorkerId = workerId,
                    ServiceId = request.ServiceId,
                    ExperienceMonths = request.ExperienceMonths,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<DalWorkerService>().AddAsync(newSkill);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}