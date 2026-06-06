using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;

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
                IdentityCardNumber = worker.IdentityCardNumber,
                AverageRating = worker.AverageRating,
                CompletedJobs = worker.CompletedJobs,
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
                IdentityCardNumber = request.IdentityCardNumber,
                AverageRating = 5.0m,
                CompletedJobs = 0,
                CreatedAt = DateTime.UtcNow
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

            _unitOfWork.Repository<WorkerProfile>().Update(worker);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<WorkerSkillDto>> GetWorkerSkillsAsync(Guid workerId)
        {
            var skills = await _unitOfWork.Repository<WorkerSkill>().FindAsync(ws => ws.WorkerId == workerId);

            return skills.Select(ws => new WorkerSkillDto
            {
                ServiceId = ws.ServiceId,
                ExperienceMonths = ws.ExperienceMonths,
                IsVerified = ws.IsVerified
            }).ToList();
        }

        public async Task<bool> AddOrUpdateWorkerSkillAsync(Guid workerId, WorkerSkillDto request)
        {
            var skill = await _unitOfWork.Repository<WorkerSkill>()
                .FirstOrDefaultAsync(ws => ws.WorkerId == workerId && ws.ServiceId == request.ServiceId);

            if (skill != null)
            {
                skill.ExperienceMonths = request.ExperienceMonths;
                _unitOfWork.Repository<WorkerSkill>().Update(skill);
            }
            else
            {
                var newSkill = new WorkerSkill
                {
                    WorkerId = workerId,
                    ServiceId = request.ServiceId,
                    ExperienceMonths = request.ExperienceMonths,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<WorkerSkill>().AddAsync(newSkill);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}