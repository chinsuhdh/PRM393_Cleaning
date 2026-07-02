using System.Text.Json;
using AutoMapper;
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
            .ForMember(destination => destination.PricingBreakdown,
                options => options.MapFrom(source => DeserializeBreakdown(source.PricingBreakdown)))
            .ForMember(destination => destination.Status,
                options => options.MapFrom(source => source.Status.ToString()))
            .ForMember(destination => destination.Notes,
                options => options.MapFrom(source => source.Notes ?? string.Empty))
            .ForMember(destination => destination.ServiceName,
                options => options.MapFrom(source => source.Service == null ? string.Empty : source.Service.Name))
            .ForMember(destination => destination.AddressText,
                options => options.MapFrom(source => source.Address == null ? string.Empty : source.Address.AddressText))
            .ForMember(destination => destination.Latitude,
                options => options.MapFrom(source => source.Address == null ? null : source.Address.Latitude))
            .ForMember(destination => destination.Longitude,
                options => options.MapFrom(source => source.Address == null ? null : source.Address.Longitude));
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
