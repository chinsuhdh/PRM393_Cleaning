using System.Text.Json;
using Cleaning.DAL.Data;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Services
{
    internal sealed class AiToolExecutor(AppDbContext context)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private const string InvalidToolError = "{\"error\":\"Công cụ không hợp lệ hoặc tham số sai.\"}";
        private const string QueryFailedError = "{\"error\":\"Không truy xuất được dữ liệu.\"}";

        public Guid? LastServiceDetailId { get; private set; }

        private static string TranslateStatus(BookingStatus status) => status switch
        {
            BookingStatus.PendingPayment => "Chờ thanh toán",
            BookingStatus.Accepted => "Nhân viên đã nhận đơn",
            BookingStatus.RescheduleRequested => "Đang yêu cầu đổi lịch",
            BookingStatus.InProgress => "Đang thực hiện",
            BookingStatus.Completed => "Hoàn thành",
            BookingStatus.Cancelled => "Đã hủy",
            BookingStatus.AwaitingWorker => "Đang tìm nhân viên",
            BookingStatus.OnTheWay => "Nhân viên đang trên đường",
            _ => status.ToString()
        };

        private static string TranslateBookingType(BookingType type) =>
            type == BookingType.Immediate ? "Đặt ngay" : "Đặt lịch hẹn";

        private static string TranslatePropertyType(PropertyType type) =>
            type == PropertyType.Apartment ? "Căn hộ" : "Nhà riêng";

        private static string TranslateUnitType(ServiceUnitType type) =>
            type == ServiceUnitType.Hour ? "Theo giờ" : type.ToString();

        public static readonly IReadOnlyList<GroqTool> ToolDefinitions =
        [
            new GroqTool
            {
                Function = new GroqFunction
                {
                    Name = "get_services",
                    Description = "Lấy danh sách các dịch vụ dọn dẹp đang hoạt động của CleanAI kèm mô tả, loại nhà, đơn vị tính và giá hiện tại.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                }
            },
            new GroqTool
            {
                Function = new GroqFunction
                {
                    Name = "get_my_bookings",
                    Description = "Lấy các đơn đặt lịch gần nhất của chính khách hàng đang hỏi: trạng thái, thời gian hẹn, dịch vụ, nhân viên phụ trách, tổng tiền và phương thức thanh toán.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                description = "Lọc theo trạng thái đơn",
                                @enum = Enum.GetNames<BookingStatus>()
                            },
                            limit = new
                            {
                                type = "integer",
                                description = "Số đơn tối đa muốn lấy (1-10), mặc định 5"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                }
            },
            new GroqTool
            {
                Function = new GroqFunction
                {
                    Name = "get_service_detail",
                    Description = "Lấy thông tin chi tiết của MỘT dịch vụ theo tên: mô tả đầy đủ, giá, số giờ tối thiểu, các tùy chọn thêm kèm phụ phí (ví dụ dọn kỹ, vệ sinh tủ lạnh) và điểm đánh giá trung bình.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            serviceName = new
                            {
                                type = "string",
                                description = "Tên (hoặc một phần tên) dịch vụ cần xem chi tiết"
                            }
                        },
                        required = new[] { "serviceName" }
                    }
                }
            },
            new GroqTool
            {
                Function = new GroqFunction
                {
                    Name = "get_service_ratings",
                    Description = "Lấy điểm đánh giá trung bình và số lượt đánh giá của từng dịch vụ, dựa trên đánh giá thật của khách hàng.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            serviceName = new
                            {
                                type = "string",
                                description = "Lọc theo tên dịch vụ (không bắt buộc)"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                }
            }
        ];

        private static readonly JsonElement EmptyArguments = JsonDocument.Parse("{}").RootElement;

        public async Task<string> ExecuteAsync(Guid userId, string toolName, string argumentsJson)
        {
            try
            {
                var root = EmptyArguments;
                if (!string.IsNullOrWhiteSpace(argumentsJson))
                {
                    using var parsed = JsonDocument.Parse(argumentsJson);
                    if (parsed.RootElement.ValueKind == JsonValueKind.Object)
                        root = parsed.RootElement.Clone();
                }

                return toolName switch
                {
                    "get_services" => await GetServicesAsync(),
                    "get_my_bookings" => await GetMyBookingsAsync(userId, root),
                    "get_service_detail" => await GetServiceDetailAsync(root),
                    "get_service_ratings" => await GetServiceRatingsAsync(root),
                    _ => InvalidToolError
                };
            }
            catch (JsonException)
            {
                return InvalidToolError;
            }
            catch (Exception)
            {
                return QueryFailedError;
            }
        }

        private async Task<string> GetServicesAsync()
        {
            var services = await context.Services
                .Where(s => s.IsActive && s.ArchivedAt == null)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.PropertyType,
                    s.UnitType,
                    BasePricePerUnitVnd = s.BasePrice,
                    s.MinimumHours
                })
                .ToListAsync();

            if (services.Count == 0)
                return "{\"message\":\"Hiện chưa có dịch vụ nào đang hoạt động.\"}";

            var result = services.Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                LoaiNha = TranslatePropertyType(s.PropertyType),
                DonViTinh = TranslateUnitType(s.UnitType),
                s.BasePricePerUnitVnd,
                SoGioToiThieu = s.MinimumHours
            });

            return JsonSerializer.Serialize(result, JsonOptions);
        }

        private async Task<string> GetMyBookingsAsync(Guid userId, JsonElement args)
        {
            var query = context.Bookings.Where(b => b.ClientId == userId);

            if (args.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse<BookingStatus>(statusProp.GetString(), true, out var status))
                    return "{\"error\":\"Trạng thái đơn không hợp lệ.\"}";
                query = query.Where(b => b.Status == status);
            }

            var limit = 5;
            if (args.TryGetProperty("limit", out var limitProp) && limitProp.ValueKind == JsonValueKind.Number && limitProp.TryGetInt32(out var requestedLimit))
                limit = Math.Clamp(requestedLimit, 1, 10);

            var rows = await query
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit)
                .Select(b => new
                {
                    b.Id,
                    ServiceName = b.Service.Name,
                    b.Status,
                    b.BookingType,
                    b.ScheduledStartTime,
                    b.ScheduledEndTime,
                    b.TotalPrice,
                    b.PaymentMethod,
                    b.WorkerId
                })
                .ToListAsync();

            if (rows.Count == 0)
                return "{\"message\":\"Khách hàng chưa có đơn đặt lịch nào phù hợp.\"}";

            var workerIds = rows.Where(r => r.WorkerId != null).Select(r => r.WorkerId!.Value).Distinct().ToList();
            var workerNames = await context.Profiles
                .Where(p => workerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.FullName);

            var result = rows.Select(r => new
            {
                r.Id,
                r.ServiceName,
                TrangThai = TranslateStatus(r.Status),
                LoaiDon = TranslateBookingType(r.BookingType),
                ScheduledStartVietnamTime = FormatVietnamTime(r.ScheduledStartTime),
                ScheduledEndVietnamTime = FormatVietnamTime(r.ScheduledEndTime),
                TotalPriceVnd = r.TotalPrice,
                PaymentMethod = r.PaymentMethod == PaymentMethod.Vnpay ? "VNPay" : "Tiền mặt",
                WorkerName = r.WorkerId != null && workerNames.TryGetValue(r.WorkerId.Value, out var name) ? name : null
            });

            return JsonSerializer.Serialize(result, JsonOptions);
        }

        private async Task<string> GetServiceDetailAsync(JsonElement args)
        {
            if (!args.TryGetProperty("serviceName", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                return InvalidToolError;

            var serviceName = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(serviceName))
                return InvalidToolError;

            var lowered = serviceName.ToLower();
            var service = await context.Services
                .Where(s => s.IsActive && s.ArchivedAt == null && s.Name.ToLower().Contains(lowered))
                .OrderBy(s => s.Name)
                .FirstOrDefaultAsync();

            if (service == null)
                return "{\"message\":\"Không tìm thấy dịch vụ nào có tên như vậy.\"}";

            LastServiceDetailId = service.Id;

            var ratingStats = await (
                from r in context.Reviews
                join b in context.Bookings on r.BookingId equals b.Id
                where b.ServiceId == service.Id && b.WorkerId != null && r.RevieweeId == b.WorkerId
                select r.Rating).ToListAsync();

            var options = new List<object>();
            try
            {
                using var schema = JsonDocument.Parse(service.BookingFormSchema);
                if (schema.RootElement.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var question in questions.EnumerateArray())
                    {
                        if (!question.TryGetProperty("label", out var label)) continue;
                        var choices = new List<object>();
                        if (question.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var opt in opts.EnumerateArray())
                            {
                                choices.Add(new
                                {
                                    TenTuyChon = opt.TryGetProperty("label", out var optLabel) ? optLabel.GetString() : null,
                                    PhuPhiVnd = opt.TryGetProperty("priceDelta", out var price) ? price.GetDecimal() : 0,
                                    ThemPhutLamViec = opt.TryGetProperty("durationDelta", out var duration) ? duration.GetInt32() : 0
                                });
                            }
                        }
                        options.Add(new { CauHoi = label.GetString(), LuaChon = choices });
                    }
                }
            }
            catch (JsonException)
            {
            }

            var detail = new
            {
                service.Id,
                service.Name,
                service.Description,
                LoaiNha = TranslatePropertyType(service.PropertyType),
                DonViTinh = TranslateUnitType(service.UnitType),
                BasePricePerUnitVnd = service.BasePrice,
                SoGioToiThieu = service.MinimumHours,
                DiemDanhGiaTrungBinh = ratingStats.Count > 0 ? Math.Round(ratingStats.Average(), 1) : (double?)null,
                SoLuotDanhGia = ratingStats.Count,
                TuyChonThem = options
            };

            return JsonSerializer.Serialize(detail, JsonOptions);
        }

        private async Task<string> GetServiceRatingsAsync(JsonElement args)
        {
            string? serviceName = null;
            if (args.TryGetProperty("serviceName", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                serviceName = nameProp.GetString();

            var query = from r in context.Reviews
                        join b in context.Bookings on r.BookingId equals b.Id
                        join s in context.Services on b.ServiceId equals s.Id
                        where b.WorkerId != null && r.RevieweeId == b.WorkerId
                        select new { s.Name, r.Rating };

            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                var lowered = serviceName.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowered));
            }

            var grouped = await query
                .GroupBy(x => x.Name)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    AverageRating = Math.Round(g.Average(x => x.Rating), 1),
                    ReviewCount = g.Count()
                })
                .OrderByDescending(x => x.AverageRating)
                .ToListAsync();

            if (grouped.Count == 0)
                return "{\"message\":\"Chưa có đánh giá nào cho dịch vụ này.\"}";

            return JsonSerializer.Serialize(grouped, JsonOptions);
        }

        private static string FormatVietnamTime(DateTime utc) =>
            utc.AddHours(7).ToString("HH:mm 'ngày' dd/MM/yyyy") + " (giờ Việt Nam)";
    }
}
