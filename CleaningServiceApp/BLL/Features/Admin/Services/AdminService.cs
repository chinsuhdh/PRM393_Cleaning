using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.BLL.Features.ServiceCatalog;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Features.Admin
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AdminService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            var totalClients = await _unitOfWork.Repository<Account>()
                .CountAsync(a => a.Role == UserRole.Client);

            var totalWorkers = await _unitOfWork.Repository<Account>()
                .CountAsync(a => a.Role == UserRole.Worker);

            var totalBookings = await _unitOfWork.Repository<Booking>()
                .CountAsync(b => true);

            var totalRevenue = await _unitOfWork.Repository<Booking>()
                .GetQueryable()
                .Where(b => b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;

            return new AdminDashboardStatsDto
            {
                TotalClients = totalClients,
                TotalWorkers = totalWorkers,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue
            };
        }

        public async Task<IEnumerable<WorkerApplicationDto>> GetWorkerApplicationsAsync()
        {
            var applications = await _unitOfWork.Repository<WorkerApplication>().GetAllAsync();
            return applications.Select(_mapper.Map<WorkerApplicationDto>);
        }

        public async Task<bool> ApproveWorkerApplicationAsync(Guid applicationId, ApproveWorkerApplicationDto dto)
        {
            var application = await _unitOfWork.Repository<WorkerApplication>().GetByIdAsync(applicationId);
            if (application == null)
                throw new AppException(AppErrors.WorkerApplicationNotFound);
            if (application.Status != WorkerApplicationStatusCodes.Pending)
                throw new AppException(AppErrors.WorkerApplicationNotPending);

            application.Status = WorkerApplicationStatusCodes.Approved;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = dto.AdminId;

            _unitOfWork.Repository<WorkerApplication>().Update(application);

            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(application.UserId);
            if (account != null)
            {
                account.Role = UserRole.Worker;
                _unitOfWork.Repository<Account>().Update(account);

                var profile = new WorkerProfile
                {
                    UserId = account.Id,
                    OnlineStatus = WorkerOnlineStatus.Offline,
                    VerificationStatus = BookingDomainConstants.WorkerVerificationStatusApproved,
                    VerifiedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<WorkerProfile>().AddAsync(profile);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectWorkerApplicationAsync(Guid applicationId, RejectWorkerApplicationDto dto)
        {
            var application = await _unitOfWork.Repository<WorkerApplication>().GetByIdAsync(applicationId);
            if (application == null)
                throw new AppException(AppErrors.WorkerApplicationNotFound);
            if (application.Status != WorkerApplicationStatusCodes.Pending)
                throw new AppException(AppErrors.WorkerApplicationNotPending);

            application.Status = WorkerApplicationStatusCodes.Rejected;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = dto.AdminId;
            application.RejectionReason = dto.Reason;

            _unitOfWork.Repository<WorkerApplication>().Update(application);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AccountAdminDto>> GetAccountsAsync()
        {
            var accounts = await _unitOfWork.Repository<Account>()
                .GetQueryable()
                .Include(a => a.Profile)
                .ToListAsync();

            return accounts.Select(_mapper.Map<AccountAdminDto>);
        }

        public async Task<bool> ChangeAccountStatusAsync(Guid accountId, UpdateAccountStatusDto dto)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId);
            if (account == null)
                throw new AppException(AppErrors.AccountNotFound);

            if (!Enum.TryParse<AccountStatus>(dto.Status, true, out var newStatus))
                throw new AppException(AppErrors.AccountStatusInvalid);

            account.Status = newStatus;
            _unitOfWork.Repository<Account>().Update(account);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
        {
            var services = await _unitOfWork.Repository<Service>().GetAllAsync();

            return services.Select(_mapper.Map<ServiceDto>);
        }

        public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto)
        {
            var service = _mapper.Map<Service>(dto);
            service.Id = Guid.NewGuid();
            service.IsActive = true;
            service.CreatedAt = DateTime.UtcNow;
            service.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Service>().AddAsync(service);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ServiceDto>(service);
        }

        public async Task<ServiceDto?> UpdateServiceAsync(Guid serviceId, UpdateServiceDto dto)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(serviceId);
            if (service == null)
                return null;

            service.Name = dto.Name;
            service.Description = dto.Description;
            service.BasePrice = dto.BasePrice;
            service.MinimumHours = dto.MinimumHours;
            service.BookingFormSchema = dto.BookingFormSchema;
            service.OperatingSchedule = dto.OperatingSchedule;
            service.IsActive = dto.IsActive;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service>().Update(service);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ServiceDto>(service);
        }

        public async Task<bool> ArchiveServiceAsync(Guid serviceId)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(serviceId);
            if (service == null)
                return false;

            service.IsActive = false;
            service.ArchivedAt = DateTime.UtcNow;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service>().Update(service);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<BookingAdminDto>> GetAllBookingsAsync()
        {
            var bookings = await _unitOfWork.Repository<Booking>().GetAllAsync();
            return bookings.Select(_mapper.Map<BookingAdminDto>);
        }
    }
}