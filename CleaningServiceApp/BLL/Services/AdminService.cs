using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Services
{
    /// <summary>
    /// Provides administrative operations such as viewing dashboard statistics, managing workers, and managing services.
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves high-level statistics for the admin dashboard, including total clients, workers, bookings, and revenue.
        /// </summary>
        /// <returns>A data transfer object containing the aggregated dashboard statistics.</returns>
        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            var totalClients = await _unitOfWork.Repository<Account>()
                .CountAsync(a => a.Role == UserRole.Client);

            var totalWorkers = await _unitOfWork.Repository<Account>()
                .CountAsync(a => a.Role == UserRole.Worker);

            var totalBookings = await _unitOfWork.Repository<Booking>()
                .CountAsync(b => true);

            var completedBookings = await _unitOfWork.Repository<Booking>()
                .FindAsync(b => b.Status == BookingStatus.Completed);

            var totalRevenue = completedBookings.Sum(b => (decimal?)b.TotalPrice) ?? 0m;

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
            return applications.Select(a => new WorkerApplicationDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Status = a.Status,
                GovernmentId = a.GovernmentId,
                ExperienceSummary = a.ExperienceSummary,
                Evidence = a.Evidence,
                SubmittedAt = a.SubmittedAt,
                ReviewedAt = a.ReviewedAt,
                ReviewedBy = a.ReviewedBy,
                RejectionReason = a.RejectionReason
            });
        }

        public async Task<bool> ApproveWorkerApplicationAsync(Guid applicationId, ApproveWorkerApplicationDto dto)
        {
            var application = await _unitOfWork.Repository<WorkerApplication>().GetByIdAsync(applicationId);
            if (application == null || application.Status != "pending")
                return false;

            application.Status = "approved";
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
                    VerificationStatus = "verified",
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
            if (application == null || application.Status != "pending")
                return false;

            application.Status = "rejected";
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

            return accounts.Select(a => new AccountAdminDto
            {
                Id = a.Id,
                Email = a.Email ?? "",
                FullName = a.Profile?.FullName,
                PhoneNumber = a.PhoneNumber,
                Role = a.Role.ToString(),
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            });
        }

        public async Task<bool> ChangeAccountStatusAsync(Guid accountId, UpdateAccountStatusDto dto)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId);
            if (account == null)
                return false;

            if (Enum.TryParse<AccountStatus>(dto.Status, true, out var newStatus))
            {
                account.Status = newStatus;
                _unitOfWork.Repository<Account>().Update(account);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
        {
            var services = await _unitOfWork.Repository<Service>().GetAllAsync();

            return services.Select(service => new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                PropertyType = service.PropertyType.ToString(),
                UnitType = service.UnitType.ToString(),
                BasePrice = service.BasePrice,
                MinimumHours = service.MinimumHours,
                IsActive = service.IsActive,
                BookingFormSchema = service.BookingFormSchema
            });
        }

        public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto)
        {
            var service = new Service
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                PropertyType = Enum.Parse<PropertyType>(dto.PropertyType, true),
                UnitType = Enum.Parse<ServiceUnitType>(dto.UnitType, true),
                BasePrice = dto.BasePrice,
                MinimumHours = dto.MinimumHours,
                IsActive = true,
                BookingFormSchema = dto.BookingFormSchema,
                OperatingSchedule = dto.OperatingSchedule,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Service>().AddAsync(service);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                PropertyType = service.PropertyType.ToString(),
                UnitType = service.UnitType.ToString(),
                BasePrice = service.BasePrice,
                MinimumHours = service.MinimumHours,
                IsActive = service.IsActive,
                BookingFormSchema = service.BookingFormSchema
            };
        }

        public async Task<ServiceDto?> UpdateServiceAsync(Guid serviceId, UpdateServiceDto dto)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(serviceId);
            if (service == null)
                return null!;

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

            return new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                PropertyType = service.PropertyType.ToString(),
                UnitType = service.UnitType.ToString(),
                BasePrice = service.BasePrice,
                MinimumHours = service.MinimumHours,
                IsActive = service.IsActive,
                BookingFormSchema = service.BookingFormSchema
            };
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
            return bookings.Select(b => new BookingAdminDto
            {
                Id = b.Id,
                ClientId = b.ClientId,
                WorkerId = b.WorkerId,
                ServiceId = b.ServiceId,
                Status = b.Status.ToString(),
                TotalPrice = b.TotalPrice,
                ScheduledStartTime = b.ScheduledStartTime,
                CreatedAt = b.CreatedAt
            });
        }
    }
}