using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Services;

/// <summary>
/// BOOK-004: computes the authoritative pricing breakdown for a service and duration. Both the quote
/// endpoint and booking creation use this so the figure the client sees equals the amount that is charged.
/// </summary>
public static class BookingPricingCalculator
{
    public static PricingBreakdownDto Calculate(Service service, decimal durationHours, decimal discountAmount)
    {
        var unitPrice = service.BasePrice;
        var lineTotal = unitPrice * durationHours;
        const decimal extraFee = 0m;

        // A client cannot lower the charge with a negative discount; the discount is also capped at the line total
        // so the total never goes below zero.
        var discount = discountAmount < 0 ? 0 : discountAmount;
        var total = lineTotal + extraFee - discount;
        if (total < 0) total = 0;

        return new PricingBreakdownDto
        {
            UnitPrice = unitPrice,
            DurationHours = durationHours,
            LineTotal = lineTotal,
            ExtraFee = extraFee,
            DiscountAmount = discount,
            TotalPrice = total,
            Currency = "VND"
        };
    }
}
