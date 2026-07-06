using System.Text.Json;
using Cleaning.BLL.Common;

namespace Cleaning.BLL.Services;

/// <summary>
/// BOOK-002: validates a client's answers against a service's <c>BookingFormSchema</c> and returns a
/// normalized JSON document containing only the known, valid answers. The API is authoritative here, so
/// this runs on the server regardless of any client-side validation.
/// </summary>
/// <remarks>
/// Schema shape:
/// <code>
/// { "questions": [
///   { "key": "rooms", "type": "number",  "required": true,  "min": 1, "max": 10 },
///   { "key": "level", "type": "choice",  "required": true,  "options": ["light","deep"] },
///   { "key": "note",  "type": "text",    "required": false, "maxLength": 200 },
///   { "key": "pets",  "type": "boolean", "required": false }
/// ] }
/// </code>
/// </remarks>
public static class BookingOptionValidator
{
    /// <param name="enforceRequired">
    /// When true (booking creation) every required question must be answered. When false (a price quote,
    /// where the client may still be completing the form) only the provided answers are checked.
    /// </param>
    public static string Normalize(
        string? schemaJson,
        IReadOnlyDictionary<string, JsonElement>? answers,
        bool enforceRequired = true)
    {
        var provided = answers ?? new Dictionary<string, JsonElement>();
        var questions = ParseQuestions(schemaJson);

        // A service without a schema accepts no free-form answers; any provided key is unknown.
        if (questions.Count == 0)
        {
            if (provided.Count > 0)
                throw new AppException(AppErrors.OptionAnswersInvalid);
            return "{}";
        }

        var knownKeys = questions.Select(question => question.Key).ToHashSet();
        if (provided.Keys.Any(key => !knownKeys.Contains(key)))
            throw new AppException(AppErrors.OptionAnswersInvalid);

        var normalized = new Dictionary<string, object?>();
        foreach (var question in questions)
        {
            if (!provided.TryGetValue(question.Key, out var value) || value.ValueKind is JsonValueKind.Null)
            {
                if (question.Required && enforceRequired)
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                continue;
            }

            normalized[question.Key] = ValidateAnswer(question, value);
        }

        return JsonSerializer.Serialize(normalized);
    }

    private static object? ValidateAnswer(SchemaQuestion question, JsonElement value)
    {
        switch (question.Type)
        {
            case "number":
            case "stepper":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                if ((question.Min.HasValue && number < question.Min.Value) ||
                    (question.Max.HasValue && number > question.Max.Value))
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                return number;

            case "boolean":
            case "yes_no":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                return value.GetBoolean();

            case "text":
                if (value.ValueKind != JsonValueKind.String)
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                var text = value.GetString() ?? string.Empty;
                if (question.MaxLength.HasValue && text.Length > question.MaxLength.Value)
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                return text;

            case "choice":
            case "single_choice":
                if (value.ValueKind != JsonValueKind.String)
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                var choice = value.GetString();
                if (choice is null || question.Options is null || !question.Options.Contains(choice))
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                return choice;

            case "multi_choice":
                if (value.ValueKind != JsonValueKind.Array || question.Options is null)
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                var choices = value.EnumerateArray().Select(item =>
                    item.ValueKind == JsonValueKind.String ? item.GetString() : null).ToArray();
                if (choices.Any(item => item is null || !question.Options.Contains(item)))
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                return choices;

            case "photos":
                if (value.ValueKind != JsonValueKind.Array)
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                var photos = value.EnumerateArray().Select(item =>
                    item.ValueKind == JsonValueKind.String ? item.GetString() : null).ToArray();
                if (photos.Any(item => item is null) || (question.Max.HasValue && photos.Length > question.Max.Value))
                    throw new AppException(AppErrors.OptionAnswersInvalid);
                return photos;

            default:
                // Unknown/unsupported question type in the schema — reject rather than silently store.
                throw new AppException(AppErrors.OptionAnswersInvalid);
        }
    }

    private static List<SchemaQuestion> ParseQuestions(string? schemaJson)
    {
        var result = new List<SchemaQuestion>();
        if (string.IsNullOrWhiteSpace(schemaJson) || schemaJson == "{}")
            return result;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException)
        {
            // A malformed schema is treated as "no questions" so a bad config cannot block bookings entirely;
            // any provided answers then fall through the unknown-key guard above.
            return result;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("questions", out var questions) ||
                questions.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var question in questions.EnumerateArray())
            {
                if (question.ValueKind != JsonValueKind.Object ||
                    !TryGetQuestionId(question, out var key) ||
                    key.ValueKind != JsonValueKind.String)
                    continue;

                result.Add(new SchemaQuestion
                {
                    Key = key.GetString()!,
                    Type = question.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String
                        ? type.GetString()!.ToLowerInvariant()
                        : "text",
                    Required = question.TryGetProperty("required", out var required) &&
                               required.ValueKind == JsonValueKind.True,
                    Min = ReadDecimal(question, "min"),
                    Max = ReadDecimal(question, "max"),
                    MaxLength = (int?)ReadDecimal(question, "maxLength"),
                    Options = ReadOptions(question)
                });
            }
        }

        return result;
    }

    private static decimal? ReadDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : null;

    private static bool TryGetQuestionId(JsonElement question, out JsonElement id)
    {
        if (question.TryGetProperty("id", out id)) return true;
        return question.TryGetProperty("key", out id);
    }

    private static HashSet<string>? ReadOptions(JsonElement question)
    {
        if (!question.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            return null;

        return options.EnumerateArray()
            .Select(option => option.ValueKind == JsonValueKind.String ? option.GetString() :
                option.ValueKind == JsonValueKind.Object && option.TryGetProperty("id", out var id) ? id.GetString() : null)
            .Where(option => option is not null)
            .Select(option => option!)
            .ToHashSet();
    }

    private sealed class SchemaQuestion
    {
        public string Key { get; init; } = null!;
        public string Type { get; init; } = "text";
        public bool Required { get; init; }
        public decimal? Min { get; init; }
        public decimal? Max { get; init; }
        public int? MaxLength { get; init; }
        public HashSet<string>? Options { get; init; }
    }
}
