using AutoMapper;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Mapping;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cleaning.BLL.Tests;

public class PaymentServiceTests
{
    [Fact(DisplayName = "[UT-BE-PAY-001-01] Thanh toán sử dụng giá booking từ máy chủ")]
    public async Task CreatePaymentAsync_UsesServerBookingPrice()
    {
        var bookingId = Guid.NewGuid();
        var bookingRepository = new Mock<IGenericRepository<Booking>>();
        var paymentRepository = new Mock<IGenericRepository<Payment>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Payment? savedPayment = null;

        bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId))
            .ReturnsAsync(new Booking
            {
                Id = bookingId,
                Status = BookingStatus.PendingPayment,
                TotalPrice = 250_000m
            });
        paymentRepository
            .Setup(repository => repository.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>()))
            .ReturnsAsync(Array.Empty<Payment>());
        paymentRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(payment => savedPayment = payment)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(work => work.Repository<Booking>()).Returns(bookingRepository.Object);
        unitOfWork.Setup(work => work.Repository<Payment>()).Returns(paymentRepository.Object);
        unitOfWork.Setup(work => work.SaveChangesAsync()).ReturnsAsync(1);

        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<BookingMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var service = new PaymentService(unitOfWork.Object, NullLogger<PaymentService>.Instance, mapper);

        var result = await service.CreatePaymentAsync(new CreatePaymentDto
        {
            BookingId = bookingId,
            Amount = 1m,
            Method = PaymentMethod.Cash
        });

        Assert.NotNull(savedPayment);
        Assert.Equal(250_000m, savedPayment.Amount);
        Assert.Equal(250_000m, result.Amount);
    }
}
