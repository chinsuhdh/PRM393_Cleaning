using System.Text.Json;
using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Features.Bookings;

public sealed class BookingCreationService(
    IUnitOfWork unitOfWork,
    IBookingAvailabilityService availabilityService,
    ILogger<BookingCreationService> logger,
    IMapper mapper) : IBookingCreationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IBookingAvailabilityService _availabilityService = availabilityService;
    private readonly ILogger<BookingCreationService> _logger = logger;
    private readonly IMapper _mapper = mapper;

    public async Task<BookingDto> CreateAsync(Guid clientId, string idempotencyKey, CreateBookingDto request)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > BookingDomainConstants.MaxIdempotencyKeyLength)
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

        var optionAnswers = BookingOptionValidator.Normalize(service.BookingFormSchema, request.OptionAnswers, logger: _logger);
        if (request.ServiceVersion != service.Version)
            throw new AppException(AppErrors.QuoteStale);
        var promotion = await GetActivePromotionAsync(service.Id);
        var durationOverride = request.DurationHours > 0 ? (decimal?)request.DurationHours : null;
        var pricing = BookingPricingCalculator.Calculate(service, optionAnswers, promotion, durationOverride);
        var durationHours = pricing.DurationHours;

        if (request.BookingType == BookingType.Immediate)
        {
            var hasActiveImmediate = await _unitOfWork.Repository<Booking>().ExistsAsync(existing =>
                existing.ClientId == clientId &&
                existing.BookingType == BookingType.Immediate &&
                existing.Status == BookingStatus.AwaitingWorker);
            if (hasActiveImmediate)
                throw new AppException(AppErrors.ImmediateBookingAlreadyActive);
        }

        var scheduledStart = request.BookingType == BookingType.Immediate
            ? DateTime.UtcNow.AddMinutes(BookingTimingConstants.ImmediateLeadMinutes)
            : request.ScheduledStartTime?.ToUniversalTime()
                ?? throw new AppException(AppErrors.StartRequired);

        await _availabilityService.ValidateAsync(clientId, new BookingAvailabilityRequestDto
        {
            ServiceId = request.ServiceId,
            AddressId = request.AddressId.Value,
            BookingType = request.BookingType,
            DurationHours = durationHours,
            From = scheduledStart,
            To = scheduledStart
        });

        var pricingBreakdown = JsonSerializer.Serialize(pricing);

        using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var initialStatus = BookingStatus.AwaitingWorker;

            var addressEntity = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(request.AddressId.Value);

            var booking = new Booking
            {
                ClientId = clientId,
                ServiceId = request.ServiceId,
                AddressId = request.AddressId,
                BookingType = request.BookingType,
                ScheduledStartTime = scheduledStart,
                ScheduledEndTime = scheduledStart.AddHours((double)durationHours),
                DurationHours = durationHours,
                UnitPrice = pricing.UnitPrice,
                ExtraFee = pricing.ExtraFee,
                DiscountAmount = pricing.DiscountAmount,
                TotalPrice = pricing.TotalPrice,
                Status = initialStatus,
                PaymentMethod = request.PaymentMethod,
                Notes = request.Notes ?? string.Empty,
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
                Reason = BookingReasons.ClientCreatedBooking,
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
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsSqlState(ex, PostgresErrorCodes.UniqueViolation))
        {
            await transaction.RollbackAsync();
            var duplicate = (await _unitOfWork.Repository<Booking>().FindAsync(
                item => item.ClientId == clientId && item.IdempotencyKey == idempotencyKey)).SingleOrDefault();
            if (duplicate != null) return _mapper.Map<BookingDto>(duplicate);
            throw new AppException(AppErrors.BookingConflict);
        }
        catch (Exception ex) when (PostgresExceptionHelpers.IsSqlState(ex, PostgresErrorCodes.SerializationFailure))
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

        var answers = BookingOptionValidator.Normalize(service.BookingFormSchema, request.OptionAnswers, enforceRequired: false, logger: _logger);
        var durationOverride = request.DurationHours > 0 ? (decimal?)request.DurationHours : null;
        return BookingPricingCalculator.Calculate(service, answers, await GetActivePromotionAsync(service.Id), durationOverride);
    }

    private async Task<Promotion?> GetActivePromotionAsync(Guid serviceId)
    {
        var now = DateTime.UtcNow;
        return (await _unitOfWork.Repository<Promotion>().FindAsync(promotion =>
            promotion.ServiceId == serviceId &&
            promotion.Status == BookingDomainConstants.PromotionStatusActive &&
            promotion.ArchivedAt == null &&
            promotion.StartsAt <= now &&
            promotion.EndsAt >= now))
            .OrderByDescending(promotion => promotion.DiscountValue)
            .FirstOrDefault();
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

}
