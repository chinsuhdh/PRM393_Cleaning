using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CleaningService.API.Common;

namespace CleaningService.API.Tests;

internal static class ApiResponseTestExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static async Task<T?> ReadDataAsync<T>(this HttpContent content)
    {
        var envelope = await content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        return envelope is null ? default : envelope.Data;
    }

    public static async Task<JsonElement> ReadEnvelopeAsync(this HttpContent content)
    {
        using var document = JsonDocument.Parse(await content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
