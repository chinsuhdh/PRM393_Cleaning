using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Cleaning.DAL.Enums;

using DalWorkerService = Cleaning.DAL.Entities.WorkerService;

namespace Cleaning.BLL.Services
{
    public class WorkerService : IWorkerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDispatchPublisher? _dispatchPublisher;

        public WorkerService(IUnitOfWork unitOfWork, IDispatchPublisher? dispatchPublisher = null)
        {
            _unitOfWork = unitOfWork;
            _dispatchPublisher = dispatchPublisher;
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
                VerifiedAt = worker.VerifiedAt,
                SuspendedAt = worker.SuspendedAt,
                PayoutBankBin = worker.PayoutBankBin,
                PayoutBankAccountNumber = worker.PayoutBankAccountNumber,
                PayoutBankAccountName = worker.PayoutBankAccountName
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

            if (_dispatchPublisher != null)
            {
                var activeBooking = await _unitOfWork.Repository<Booking>().FirstOrDefaultAsync(
                    b => b.WorkerId == workerId &&
                         (b.Status == BookingStatus.Accepted || b.Status == BookingStatus.OnTheWay));
                if (activeBooking != null)
                    await _dispatchPublisher.WorkerPositionAsync(activeBooking.Id, request.CurrentLat, request.CurrentLng);
            }

            return true;
        }

        public async Task<bool> UpdateOnlineStatusAsync(Guid workerId, UpdateOnlineStatusDto request)
        {
            if (request.OnlineStatus == WorkerOnlineStatus.Busy)
                throw new InvalidOperationException("Chỉ có thể chuyển trạng thái Online hoặc Offline.");

            var worker = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
            if (worker == null) return false;

            if (worker.SuspendedAt.HasValue && request.OnlineStatus != WorkerOnlineStatus.Offline)
                throw new AppException(AppErrors.WorkerSuspended);

            if (worker.OnlineStatus == WorkerOnlineStatus.Busy && request.OnlineStatus != WorkerOnlineStatus.Offline)
                throw new InvalidOperationException("Không thể chuyển sang Online khi đang có công việc.");

            worker.OnlineStatus = request.OnlineStatus;
            worker.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<WorkerProfile>().Update(worker);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<WorkerSkillDto>> GetWorkerSkillsAsync(Guid workerId)
        {
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

        public async Task<bool> UpdatePayoutAccountAsync(Guid workerId, UpdatePayoutAccountDto request)
        {
            var bankBin = request.BankBin?.Trim();
            var accountNumber = request.AccountNumber?.Trim();
            var accountName = request.AccountName?.Trim();
            if (string.IsNullOrEmpty(bankBin) || string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(accountName))
                throw new AppException(AppErrors.PayoutAccountInvalid);

            var worker = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
            if (worker == null) return false;

            worker.PayoutBankBin = bankBin;
            worker.PayoutBankAccountNumber = accountNumber;
            worker.PayoutBankAccountName = accountName;
            worker.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<WorkerProfile>().Update(worker);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<WorkerEarningDto>> GetWorkerEarningsAsync(Guid workerId)
        {
            var earnings = await _unitOfWork.Repository<WorkerEarning>().FindAsync(e => e.WorkerId == workerId);

            return earnings
                .OrderByDescending(e => e.EarnedAt)
                .Select(e => new WorkerEarningDto
                {
                    Id = e.Id,
                    BookingId = e.BookingId,
                    Amount = e.Amount,
                    Status = e.Status,
                    EarnedAt = e.EarnedAt,
                    PaidAt = e.PaidAt,
                    PayoutFailureReason = e.PayoutFailureReason
                })
                .ToList();
        }
    }
}