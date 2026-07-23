using System.Text.Json;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Mapping;

public sealed class BookingMappingProfile : AutoMapper.Profile
{
    public BookingMappingProfile()
    {
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
                options => options.MapFrom(source => source.Worker == null ? null : new WorkerSummaryDto
                {
                    Id = source.Worker.UserId,
                    Name = source.Worker.User == null ? string.Empty : source.Worker.User.FullName,
                    AvatarUrl = source.Worker.User == null ? null : source.Worker.User.AvatarUrl,
                    Rating = source.Worker.AverageRating,
                    Latitude = source.Worker.CurrentLat,
                    Longitude = source.Worker.CurrentLng,
                    LocationUpdatedAt = source.Worker.LocationUpdatedAt
                }));

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
