using System.Net;
using System.Net.Http.Json;
using Cleaning.BLL.DTOs;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class ServiceCatalogApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BE-SERVICE-001-01] API danh mục trả về các nhóm dịch vụ")]
    public async Task GetCategories_ReturnsConfiguredCategories()
    {
        var response = await fixture.Client.GetAsync("/api/ServiceCatalog/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<ServiceCategoryDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(categories);
        Assert.Equal(2, categories.Count);
    }
}
