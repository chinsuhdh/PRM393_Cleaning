using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            // Đếm số lượng User (Client)
            var totalClients = await _unitOfWork.Repository<Account>()
                .GetQueryable()
                .CountAsync(a => a.Role == UserRole.Client);

            // Đếm số lượng Thợ (Worker)
            var totalWorkers = await _unitOfWork.Repository<Account>()
                .GetQueryable()
                .CountAsync(a => a.Role == UserRole.Worker);

            // Đếm tổng số Đơn hàng
            var totalBookings = await _unitOfWork.Repository<Booking>()
                .GetQueryable()
                .CountAsync();

            // Tính tổng doanh thu (Những đơn đã Completed)
            var totalRevenue = await _unitOfWork.Repository<Booking>()
                .GetQueryable()
                .Where(b => b.Status == BookingStatus.Completed)
                .SumAsync(b => b.TotalPrice);

            return new AdminDashboardStatsDto
            {
                TotalClients = totalClients,
                TotalWorkers = totalWorkers,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue
            };
        }
    }
}