using System.Text.Json;
using Cleaning.BLL.Features.Payments;
using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Bookings;

public sealed class BookingMappingProfile : AutoMapper.Profile
{
    public BookingMappingProfile()
    {
        CreateMap<WorkerProfile, WorkerSummaryDto>()
            .ForMember(destination => destination.Id,
                options => options.MapFrom(source => source.UserId))
            .ForMember(destination => destination.Name,
                options => options.MapFrom(source => source.User == null ? string.Empty : source.User.FullName))
            .ForMember(destination => destination.AvatarUrl,
                options => options.MapFrom(source => source.User == null ? null : source.User.AvatarUrl))
            .ForMember(destination => destination.Rating,
                options => options.MapFrom(source => source.AverageRating))
            .ForMember(destination => destination.Latitude,
                options => options.MapFrom(source => source.CurrentLat))
            .ForMember(destination => destination.Longitude,
                options => options.MapFrom(source => source.CurrentLng));

        CreateMap<Booking, BookingDto>()
            .ForMember(destination => destination.BookingType,
                options => options.MapFrom(source => source.BookingType.ToString()))
            .ForMember(destination => destination.OptionAnswers,
                options => options.MapFrom(source => string.IsNullOrWhiteSpace(source.OptionAnswers) ? "{}" : source.OptionAnswers))
            .ForMember(destination => destination.BookingFormSchema,
                options => options.MapFrom(source => source.Service == null ? "{}" : source.Service.BookingFormSchema))
            .ForMember(destination => destination.PricingBreakdown,
                options => options.MapFrom(source => DeserializeBreakdown(source.PricingBreakdown)))
            .ForMember(destination => destination.Status,
                options => options.MapFrom(source => source.Status.ToString()))
            .ForMember(destination => destination.PaymentMethod,
                options => options.MapFrom(source => source.PaymentMethod.ToString()))
            .ForMember(destination => destination.Notes,
                options => options.MapFrom(source => source.Notes ?? string.Empty))
            .ForMember(destination => destination.ServiceName,
                options => options.MapFrom(source => source.Service == null ? string.Empty : source.Service.Name))
            .ForMember(destination => destination.AddressText,
                options => options.MapFrom(source => source.Address == null ? string.Empty : source.Address.AddressText))
            .ForMember(destination => destination.Latitude,
                options => options.MapFrom(source => source.Address == null ? null : source.Address.Latitude))
            .ForMember(destination => destination.Longitude,
                options => options.MapFrom(source => source.Address == null ? null : source.Address.Longitude))
            .ForMember(destination => destination.Worker,
                options => options.MapFrom(source => source.Worker))
            .ForMember(destination => destination.DistanceKm, options => options.Ignore())
            .ForMember(destination => destination.EstimatedMinutes, options => options.Ignore())
            .ForMember(destination => destination.Photos, options => options.Ignore())
            .ForMember(destination => destination.StatusTimeline, options => options.Ignore())
            .ForMember(destination => destination.PendingReschedule, options => options.Ignore())
            .ForMember(destination => destination.RescheduleHistory, options => options.Ignore());

        CreateMap<Payment, PaymentDto>()
            .ForMember(destination => destination.Method,
                options => options.MapFrom(source => source.Method.ToString()))
            .ForMember(destination => destination.Status,
                options => options.MapFrom(source => source.Status.ToString()));
    }

    private static PricingBreakdownDto? DeserializeBreakdown(string? breakdown)
    {
        if (string.IsNullOrWhiteSpace(breakdown) || breakdown == "{}")
            return null;
        try
        {
            return JsonSerializer.Deserialize<PricingBreakdownDto>(breakdown);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
