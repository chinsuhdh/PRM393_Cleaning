using Cleaning.BLL.Features.ServiceCatalog;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

// D.3: the wizard's questions step reads `bookingFormSchema` off the service the catalog API returns.
// If ServiceDto ever drops that field, Step 1 silently renders no questions with no error anywhere.
public sealed class ServiceCatalogServiceTests
{
    private const string Schema = "{\"questions\":[{\"id\":\"rooms\",\"type\":\"stepper\"}]}";
    private static readonly Guid HouseCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact(DisplayName = "[UT-SVC-CAT-01] GetServiceByIdAsync surfaces the BookingFormSchema so the wizard can render questions")]
    public async Task GetServiceById_IncludesBookingFormSchema()
    {
        var service = BuildService(PropertyType.Apartment);
        var unitOfWork = new InMemoryUnitOfWork().With([service]);
        var catalogService = new ServiceCatalogService(unitOfWork, TestMapperFactory.Create(), NullLogger<ServiceCatalogService>.Instance);

        var dto = await catalogService.GetServiceByIdAsync(service.Id);

        Assert.NotNull(dto);
        Assert.Equal(Schema, dto!.BookingFormSchema);
    }

    [Fact(DisplayName = "[UT-SVC-CAT-02] GetServicesByCategoryIdAsync also surfaces the BookingFormSchema")]
    public async Task GetServicesByCategory_IncludesBookingFormSchema()
    {
        var service = BuildService(PropertyType.House);
        var unitOfWork = new InMemoryUnitOfWork().With([service]);
        var catalogService = new ServiceCatalogService(unitOfWork, TestMapperFactory.Create(), NullLogger<ServiceCatalogService>.Instance);

        var results = await catalogService.GetServicesByCategoryIdAsync(HouseCategoryId);

        var dto = Assert.Single(results);
        Assert.Equal(Schema, dto.BookingFormSchema);
    }

    private static Service BuildService(PropertyType propertyType) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Cleaning",
        PropertyType = propertyType,
        UnitType = ServiceUnitType.Hour,
        BasePrice = 100_000,
        MinimumHours = 2,
        IsActive = true,
        BookingFormSchema = Schema
    };
}
