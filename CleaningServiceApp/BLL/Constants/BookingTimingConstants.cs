namespace Cleaning.BLL.Constants;

public static class BookingTimingConstants
{
    public const int ImmediateLeadMinutes = 15;
    public const int ScheduledLeadHours = 2;
    public const int TravelBufferMinutes = 30;
    public const int LocationFreshnessMinutes = 10;
    public const int SlotIntervalMinutes = 30;
    public const int ImmediateSlotRoundingMinutes = 5;
    public const int ImmediateSlotCap = 1;
    public const int ScheduledSlotCap = 12;
    public const int QuoteValidityMinutes = 2;
    public const int MaxAdvanceSchedulingDays = 30;
    public const int PreStartResponseCutoffHours = 1;
}
