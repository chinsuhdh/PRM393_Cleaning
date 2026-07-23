namespace CleaningService.API.Common;

public static class DispatchGroups
{
    public static string Worker(Guid workerId) => $"worker:{workerId}";
    public static string Booking(Guid bookingId) => $"booking:{bookingId}";
    public static string Client(Guid clientId) => $"client:{clientId}";
}

public static class DispatchConstants
{
    public const string DispatchHubPath = "/hubs/dispatch";
}
