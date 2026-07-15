namespace Cleaning.BLL.Constants;

public static class CancellationConstants
{
    public const int SuspensionWindowDays = 30;
    public const int SuspensionStrikeThreshold = 3;
    public const int ReportMinFreeTextLength = 20;
}

public static class WorkerCancelReasonCodes
{
    public const string Prefix = "worker_cancel.";
    public const string ScheduleConflict = Prefix + "schedule_conflict";
    public const string TooFar = Prefix + "too_far";
    public const string Other = Prefix + "other";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [ScheduleConflict] = "Trùng lịch trình",
        [TooFar] = "Địa chỉ quá xa",
        [Other] = "Lý do khác",
    };

    public static readonly IReadOnlySet<string> All = Labels.Keys.ToHashSet();
}

public static class ClientCancelReasonCodes
{
    public const string Prefix = "client_cancel.";
    public const string NoLongerNeeded = Prefix + "no_longer_needed";
    public const string FoundAnotherProvider = Prefix + "found_another_provider";
    public const string Other = Prefix + "other";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [NoLongerNeeded] = "Không còn cần dịch vụ",
        [FoundAnotherProvider] = "Đã tìm được đơn vị khác",
        [Other] = "Lý do khác",
    };

    public static readonly IReadOnlySet<string> All = Labels.Keys.ToHashSet();
}

// H.1: report reason codes, separated per reporting role — a client reports a worker, a worker
// reports a client — sharing the same "report.*" varchar column and the same POST {id}/report
// endpoint (BookingService.Report.cs), but validated against the caller's own role-specific set.
public static class ReportReasonCodes
{
    public const string Prefix = "report.";

    public static readonly IReadOnlyDictionary<string, string> ClientCodes = new Dictionary<string, string>
    {
        ["report.client.worker_no_show"] = "Nhân viên không đến",
        ["report.client.worker_rude"] = "Nhân viên có thái độ không tốt",
        ["report.client.worker_poor_quality"] = "Chất lượng dịch vụ kém",
        ["report.client.other"] = "Lý do khác",
    };

    public static readonly IReadOnlyDictionary<string, string> WorkerCodes = new Dictionary<string, string>
    {
        ["report.worker.client_absent"] = "Khách hàng vắng mặt",
        ["report.worker.unsafe_environment"] = "Môi trường làm việc không an toàn",
        ["report.worker.client_abusive"] = "Khách hàng có hành vi không đúng mực",
        ["report.worker.other"] = "Lý do khác",
    };
}
