using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;
using System.Text.Json;

namespace Cleaning.BLL.Services;

/// <summary>
/// BOOK-004: computes the authoritative pricing breakdown for a service and duration. Both the quote
/// endpoint and booking creation use this so the figure the client sees equals the amount that is charged.
/// </summary>
public static class BookingPricingCalculator
{
    public static PricingBreakdownDto Calculate(Service service, string normalizedAnswers, Promotion? promotion = null)
    {
        var (durationHours, answerLines) = CalculateAnswerAdjustments(service, normalizedAnswers);
        var unitPrice = service.BasePrice;
        var lineTotal = unitPrice * durationHours;
        const decimal extraFee = 0m;

        // A client cannot lower the charge with a negative discount; the discount is also capped at the line total
        // so the total never goes below zero.
        var lines = new List<PricingLineDto>
        {
            new() { Label = $"{service.Name} — base", Amount = service.BasePrice * service.MinimumHours }
        };
        lines.AddRange(answerLines);
        var subtotal = lines.Sum(line => line.Amount);
        var discount = promotion == null ? 0 : promotion.DiscountType.Equals("percentage", StringComparison.OrdinalIgnoreCase)
            ? decimal.Round(subtotal * promotion.DiscountValue / 100m, 0)
            : promotion.DiscountValue;
        discount = Math.Clamp(discount, 0, subtotal);
        if (promotion != null && discount > 0)
            lines.Add(new PricingLineDto { Label = promotion.Name, Amount = -discount });
        var total = subtotal - discount;

        return new PricingBreakdownDto
        {
            UnitPrice = unitPrice,
            DurationHours = durationHours,
            LineTotal = lineTotal,
            ExtraFee = extraFee,
            DiscountAmount = discount,
            TotalPrice = total,
            Currency = "VND",
            ServiceVersion = service.Version,
            Breakdown = lines
        };
    }

    private static (decimal DurationHours, List<PricingLineDto> Lines) CalculateAnswerAdjustments(Service service, string answersJson)
    {
        decimal minutes = service.MinimumHours * 60;
        var lines = new List<PricingLineDto>();
        using var answers = JsonDocument.Parse(answersJson);
        using var schema = JsonDocument.Parse(string.IsNullOrWhiteSpace(service.BookingFormSchema) ? "{}" : service.BookingFormSchema);
        if (!schema.RootElement.TryGetProperty("questions", out var questions)) return (service.MinimumHours, lines);
        foreach (var question in questions.EnumerateArray())
        {
            var id = question.TryGetProperty("id", out var idNode) ? idNode.GetString() : question.TryGetProperty("key", out var keyNode) ? keyNode.GetString() : null;
            if (id == null || !answers.RootElement.TryGetProperty(id, out var answer)) continue;
            var type = question.GetProperty("type").GetString();
            if (type is "stepper" or "number")
            {
                var source = question.TryGetProperty("unit", out var unit) ? unit : question;
                AddDelta(source, answer.GetDecimal(), question, lines, ref minutes);
            }
            else if (type is "single_choice" or "choice") AddSelected(question, answer.GetString(), lines, ref minutes);
            else if (type == "multi_choice") foreach (var item in answer.EnumerateArray()) AddSelected(question, item.GetString(), lines, ref minutes);
        }
        return (Math.Ceiling(minutes / 30m) * 30m / 60m, lines);
    }

    private static void AddSelected(JsonElement question, string? selected, List<PricingLineDto> lines, ref decimal minutes)
    {
        if (selected == null || !question.TryGetProperty("options", out var options)) return;
        foreach (var option in options.EnumerateArray())
            if (option.ValueKind == JsonValueKind.Object && option.TryGetProperty("id", out var id) && id.GetString() == selected)
                AddDelta(option, 1, option, lines, ref minutes);
    }

    private static void AddDelta(JsonElement source, decimal multiplier, JsonElement labelSource, List<PricingLineDto> lines, ref decimal minutes)
    {
        minutes += source.TryGetProperty("durationDelta", out var duration) ? duration.GetDecimal() * multiplier : 0;
        var amount = source.TryGetProperty("priceDelta", out var price) ? price.GetDecimal() * multiplier : 0;
        if (amount != 0) lines.Add(new PricingLineDto { Label = labelSource.TryGetProperty("label", out var label) ? label.GetString()! : "Option", Amount = amount });
    }
}
