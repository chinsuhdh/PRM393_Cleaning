using System.Text.Json;
using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services;

public sealed class BookingCreationService(
    IUnitOfWork unitOfWork,
    IBookingAvailabilityService availabilityService,
    ILogger<BookingCreationService> logger,
    IMapper mapper) : IBookingCreationService
{
    private const int ImmediateLeadMinutes = 15;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IBookingAvailabilityService _availabilityService = availabilityService;
    private readonly ILogger<BookingCreationService> _logger = logger;
    private readonly IMapper _mapper = mapper;

    public async Task<BookingDto> CreateAsync(Guid clientId, string idempotencyKey, CreateBookingDto request)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new AppException(AppErrors.IdempotencyKeyRequired);
        var existing = (await _unitOfWork.Repository<Booking>().FindAsync(
            item => item.ClientId == clientId && item.IdempotencyKey == idempotencyKey)).SingleOrDefault();
        if (existing != null) return _mapper.Map<BookingDto>(existing);

        var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId)
                        ?? throw new AppException(AppErrors.ServiceUnavailable);
        if (!service.IsActive)
            throw new AppException(AppErrors.ServiceUnavailable);
        if (!request.AddressId.HasValue)
            throw new AppException(AppErrors.AddressRequired);

        // BOOK-002: validate the service-defined answers against the service schema before any write.
        var optionAnswers = BookingOptionValidator.Normalize(service.BookingFormSchema, request.OptionAnswers);

        var scheduledStart = request.BookingType == BookingType.Immediate
            ? DateTime.UtcNow.AddMinutes(ImmediateLeadMinutes)
            : request.ScheduledStartTime?.ToUniversalTime()
                ?? throw new AppException(AppErrors.StartRequired);

        await _availabilityService.ValidateAsync(clientId, new BookingAvailabilityRequestDto
        {
            ServiceId = request.ServiceId,
            AddressId = request.AddressId.Value,
            BookingType = request.BookingType,
            DurationHours = request.DurationHours,
            From = scheduledStart,
            To = scheduledStart
        });

        // Time legality (BOOK-003): a Scheduled booking must fall within the service's operating hours.
        // Worker availability is intentionally NOT checked here — matching happens afterwards via dispatch,
        // so the booking is created first and offered to eligible workers who can accept it.
        if (request.BookingType == BookingType.Scheduled &&
            !BookingAvailabilityService.IsWithinOperatingSchedule(
                service, scheduledStart, scheduledStart.AddHours((double)request.DurationHours)))
            throw new AppException(AppErrors.OutsideOperatingHours);

        // BOOK-004: the server computes the authoritative pricing breakdown; the client only displays it.
        var pricing = BookingPricingCalculator.Calculate(service, request.DurationHours, request.DiscountAmount);
        var pricingBreakdown = JsonSerializer.Serialize(pricing);

        using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var initialStatus = request.BookingType == BookingType.Immediate
                ? BookingStatus.AwaitingWorker
                : BookingStatus.PendingPayment;

            var addressEntity = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(request.AddressId.Value);

            var booking = new Booking
            {
                ClientId = clientId,
                ServiceId = request.ServiceId,
                AddressId = request.AddressId,
                BookingType = request.BookingType,
                ScheduledStartTime = scheduledStart,
                ScheduledEndTime = scheduledStart.AddHours((double)request.DurationHours),
                DurationHours = request.DurationHours,
                UnitPrice = pricing.UnitPrice,
                ExtraFee = pricing.ExtraFee,
                DiscountAmount = pricing.DiscountAmount,
                TotalPrice = pricing.TotalPrice,
                Status = initialStatus,
                Notes = request.Notes ?? string.Empty, // Fix: Tránh gán null
                OptionAnswers = optionAnswers,
                PricingBreakdown = pricingBreakdown,
                AddressSnapshot = BuildAddressSnapshot(addressEntity),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IdempotencyKey = idempotencyKey
            };

            await _unitOfWork.Repository<Booking>().AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            var statusLog = new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = null,
                NewStatus = initialStatus,
                ChangedBy = clientId,
                Reason = "Khách hàng tạo đơn đặt lịch",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
            await _unitOfWork.SaveChangesAsync();

            await transaction.CommitAsync();

            booking.Service = service;
            booking.Address = addressEntity;

            return _mapper.Map<BookingDto>(booking);
        }
        catch (AppException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Xung ??t khi t?o Booking cho ClientId: {ClientId}", clientId);
            throw new AppException(AppErrors.BookingConflict);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync();
            var duplicate = (await _unitOfWork.Repository<Booking>().FindAsync(
                item => item.ClientId == clientId && item.IdempotencyKey == idempotencyKey)).SingleOrDefault();
            if (duplicate != null) return _mapper.Map<BookingDto>(duplicate);
            throw new AppException(AppErrors.BookingConflict);
        }
        catch (DbUpdateException ex) when (IsSerializationFailure(ex))
        {
            await transaction.RollbackAsync();
            throw new AppException(AppErrors.BookingConflict);
        }
        catch (Exception ex) when (IsPostgresSqlState(ex, "40001"))
        {
            await transaction.RollbackAsync();
            throw new AppException(AppErrors.BookingConflict);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi xảy ra khi tạo Booking cho ClientId: {ClientId}", clientId);
            throw new AppException(AppErrors.BookingCreateFailed, ex);
        }
    }

    public async Task<PricingBreakdownDto> GetQuoteAsync(Guid clientId, BookingQuoteRequestDto request)
    {
        var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId);
        if (service == null || !service.IsActive || service.ArchivedAt.HasValue)
            throw new AppException(AppErrors.ServiceUnavailable);

        if (request.DurationHours < service.MinimumHours)
            throw new AppException(AppErrors.DurationInvalid);

        // Validate any provided answers (types/choices/unknown keys) but do not require completeness:
        // the client may request a quote while still filling in the form.
        BookingOptionValidator.Normalize(service.BookingFormSchema, request.OptionAnswers, enforceRequired: false);

        return BookingPricingCalculator.Calculate(service, request.DurationHours, request.DiscountAmount);
    }

    private static string BuildAddressSnapshot(UserAddress? address)
    {
        if (address == null) return "{}";
        return JsonSerializer.Serialize(new
        {
            addressId = address.Id,
            label = address.Label,
            addressText = address.AddressText,
            latitude = address.Latitude,
            longitude = address.Longitude
        });
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException?.GetType().Name == "PostgresException" &&
        exception.InnerException.GetType().GetProperty("SqlState")?.GetValue(exception.InnerException)?.ToString() == "23505";

    private static bool IsSerializationFailure(DbUpdateException exception) =>
        IsPostgresSqlState(exception, "40001");

    private static bool IsPostgresSqlState(Exception exception, string sqlState)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current.GetType().Name == "PostgresException" &&
                current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString() == sqlState)
                return true;
        }
        return false;
    }
}
