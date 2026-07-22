using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace CleaningService.API.Data;

internal static class DevelopmentSeedData
{
    internal const string DevelopmentPassword = "CleanAI123!";

    internal static readonly Guid ApartmentServiceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid HouseServiceId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static readonly Guid AdminId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    internal static readonly Guid ClientId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    internal static readonly Guid WorkerId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    internal static readonly Guid ClientAddressId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    internal static readonly Guid WorkerAvailabilityId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    internal static Service CreateApartmentService(DateTime now) => new()
    {
        Id = ApartmentServiceId,
        Name = "Dọn dẹp căn hộ",
        Description = "Dịch vụ dọn dẹp cơ bản và chuyên sâu cho căn hộ chung cư.",
        PropertyType = PropertyType.Apartment,
        UnitType = ServiceUnitType.Hour,
        BasePrice = 120_000m,
        MinimumHours = 2,
        IsActive = true,
        BookingFormSchema = ApartmentBookingFormSchema,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static Service CreateHouseService(DateTime now) => new()
    {
        Id = HouseServiceId,
        Name = "Dọn dẹp nhà phố",
        Description = "Dịch vụ dọn dẹp cơ bản và chuyên sâu cho nhà phố.",
        PropertyType = PropertyType.House,
        UnitType = ServiceUnitType.Hour,
        BasePrice = 150_000m,
        MinimumHours = 3,
        IsActive = true,
        BookingFormSchema = HouseBookingFormSchema,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal const string ApartmentBookingFormSchema = """
        {
          "questions": [
            { "id": "level", "type": "single_choice", "label": "Mức độ dọn dẹp", "required": true,
              "options": [
                { "id": "light", "label": "Dọn cơ bản" },
                { "id": "deep", "label": "Dọn kỹ", "priceDelta": 50000, "durationDelta": 30 } ] },
            { "id": "addons", "type": "multi_choice", "label": "Dịch vụ thêm",
              "options": [
                { "id": "fridge", "label": "Vệ sinh tủ lạnh", "priceDelta": 30000, "durationDelta": 20 },
                { "id": "windows", "label": "Lau cửa kính", "priceDelta": 50000, "durationDelta": 30 } ] },
            { "id": "pets", "type": "yes_no", "label": "Nhà bạn có nuôi thú cưng không?" },
            { "id": "note", "type": "text", "label": "Ghi chú cho nhân viên", "maxLength": 500 },
            { "id": "photos", "type": "photos", "label": "Hình ảnh không gian cần dọn", "max": 5 }
          ]
        }
        """;

    internal const string HouseBookingFormSchema = """
        {
          "questions": [
            { "id": "floors", "type": "stepper", "label": "Nhà có bao nhiêu tầng?",
              "min": 1, "max": 5, "required": true,
              "unit": { "priceDelta": 60000, "durationDelta": 60 } },
            { "id": "level", "type": "single_choice", "label": "Mức độ dọn dẹp", "required": true,
              "options": [
                { "id": "light", "label": "Dọn cơ bản" },
                { "id": "deep", "label": "Dọn kỹ", "priceDelta": 70000, "durationDelta": 45 } ] },
            { "id": "addons", "type": "multi_choice", "label": "Dịch vụ thêm",
              "options": [
                { "id": "oven", "label": "Vệ sinh lò nướng", "priceDelta": 40000, "durationDelta": 25 },
                { "id": "garage", "label": "Nhà để xe", "priceDelta": 70000, "durationDelta": 40 } ] },
            { "id": "garden", "type": "yes_no", "label": "Nhà có sân vườn không?" },
            { "id": "pets", "type": "yes_no", "label": "Nhà bạn có nuôi thú cưng không?" },
            { "id": "note", "type": "text", "label": "Ghi chú cho nhân viên", "maxLength": 500 },
            { "id": "photos", "type": "photos", "label": "Hình ảnh không gian cần dọn", "max": 5 },
            { "id": "future", "type": "hologram", "label": "Câu hỏi ẩn từ tương lai" }
          ]
        }
        """;

    internal static Account CreateAccount(Guid id, string email, UserRole role, DateTime now) => new()
    {
        Id = id,
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(DevelopmentPassword),
        Role = role,
        Status = AccountStatus.Active,
        IsEmailVerified = true,
        IsPhoneVerified = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static Profile CreateProfile(Guid id, string fullName, DateTime now) => new()
    {
        Id = id,
        FullName = fullName,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static UserAddress CreateClientAddress(DateTime now) => new()
    {
        Id = ClientAddressId,
        UserId = ClientId,
        Label = "Home",
        AddressText = "1 Nguyen Hue, District 1, Ho Chi Minh City",
        Latitude = 10.7731m,
        Longitude = 106.7030m,
        PropertyType = PropertyType.Apartment,
        IsDefault = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerProfile CreateWorkerProfile(DateTime now) => new()
    {
        UserId = WorkerId,
        AverageRating = 5.0m,
        OnlineStatus = WorkerOnlineStatus.Online,
        CurrentLat = 10.7769m,
        CurrentLng = 106.7009m,
        LocationUpdatedAt = now,
        BaseLatitude = 10.7769m,
        BaseLongitude = 106.7009m,
        ServiceRadiusKm = 10m,
        VerifiedAt = now,
        VerificationStatus = "approved",
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerService CreateWorkerService(Guid serviceId, int experienceMonths, DateTime now) => new()
    {
        WorkerId = WorkerId,
        ServiceId = serviceId,
        ExperienceMonths = experienceMonths,
        IsVerified = true,
        VerifiedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerAvailability CreateWorkerAvailability(DateTime now) => new()
    {
        Id = WorkerAvailabilityId,
        WorkerId = WorkerId,
        StartTime = now.Date.AddDays(1).AddHours(8),
        EndTime = now.Date.AddDays(30).AddHours(18),
        Status = AvailabilityStatus.Available,
        Note = "Development seed availability",
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static IEnumerable<KnowledgeDocument> CreateKnowledgeDocuments(DateTime now)
    {
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000001",
            "Chính sách hủy đặt lịch",
            "Trước khi có nhân viên nhận việc, khách hàng có thể hủy bất cứ lúc nào miễn phí vì chưa có khoản thanh toán nào được thực hiện. Sau khi nhân viên đã nhận việc, khách hàng không thể tự hủy trực tiếp mà cần gửi báo cáo (report) nêu lý do; hệ thống sẽ hủy đơn và ghi nhận để admin xem xét. Không có phí phạt và không có tiền hoàn lại vì tiền chỉ được thu sau khi hoàn thành công việc.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000002",
            "Dịch vụ hiện có",
            "CleanAI cung cấp dịch vụ dọn dẹp căn hộ (apartment) và nhà phố (house), tính phí theo giờ. Khách hàng chọn dịch vụ, trả lời các câu hỏi liên quan (diện tích, số phòng, yêu cầu đặc biệt...) để hệ thống tính giá chính xác trước khi đặt.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000003",
            "Cách ghép nhân viên",
            "Đặt lịch ngay (immediate): hệ thống gửi thông báo cho các nhân viên đang online, phù hợp dịch vụ và trong bán kính hoạt động; nhân viên nào nhận trước sẽ được giao việc, tối đa chờ 5 phút. Đặt lịch hẹn trước (scheduled): công việc hiển thị trong danh sách của nhân viên phù hợp cho đến 1 giờ trước giờ hẹn; nếu không có ai nhận, đơn sẽ tự động bị hủy và khách được thông báo.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000004",
            "Cách đặt lịch",
            "Khách hàng chọn dịch vụ trên trang chủ, trả lời các câu hỏi của dịch vụ, chọn địa chỉ, sau đó chọn đặt Ngay (Immediate) hoặc Hẹn giờ (Scheduled) trong tương lai. Hệ thống báo giá chi tiết trước khi xác nhận. Đặt lịch ngay sẽ mở màn hình tìm nhân viên tối đa 5 phút; đặt lịch hẹn trước sẽ hiển thị 'Yêu cầu đã được đăng' và không cần chờ tại màn hình.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000005",
            "Thanh toán",
            "CleanAI chỉ thu tiền SAU KHI công việc hoàn thành — không thu tiền trước, do đó không có chính sách hoàn tiền. Có hai hình thức: tiền mặt (nhân viên xác nhận đã nhận tiền ngay tại chỗ) hoặc VNPay (khách thanh toán qua cổng VNPay ngay trong ứng dụng sau khi nhân viên bấm Hoàn thành). Nhân viên nhận 100% giá trị đơn hàng, không có phí hoa hồng nền tảng.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000006",
            "Đổi lịch hẹn",
            "Chỉ áp dụng cho đơn đặt lịch hẹn trước (scheduled) đã có nhân viên nhận việc. Một trong hai bên (khách hoặc nhân viên) có thể đề nghị đổi sang thời gian mới; bên còn lại phải chấp nhận thì thời gian mới mới có hiệu lực, nếu từ chối thì giữ nguyên giờ cũ. Yêu cầu đổi lịch sẽ tự động bị từ chối nếu không có phản hồi trước 1 giờ so với giờ hẹn hiện tại. Chỉ có thể có một yêu cầu đổi lịch đang chờ xử lý tại một thời điểm.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000007",
            "Theo dõi và hoàn thành công việc",
            "Sau khi nhân viên nhận việc, khách có thể trò chuyện (chat) trực tiếp với nhân viên trong ứng dụng. Khi nhân viên bấm 'Đang di chuyển', khách sẽ thấy bản đồ theo dõi trực tiếp vị trí nhân viên. Khi đến gần địa chỉ, nhân viên bấm 'Bắt đầu công việc'. Khi xong việc, nhân viên bấm 'Hoàn thành' để chuyển sang bước thanh toán.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000008",
            "Đánh giá nhân viên",
            "Sau khi đơn hoàn tất, khách hàng được mời đánh giá nhân viên từ 1 đến 5 sao kèm nhận xét (không bắt buộc). Mỗi đơn chỉ được đánh giá một lần nhưng có thể chỉnh sửa lại bất cứ lúc nào. Điểm đánh giá trung bình của nhân viên hiển thị trên hồ sơ và thẻ công việc.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000009",
            "Trở thành nhân viên CleanAI",
            "Khách hàng có thể đăng ký trở thành nhân viên từ mục Hồ sơ/Cài đặt bằng cách điền đơn (dịch vụ muốn nhận, khu vực hoạt động, kinh nghiệm) và tải ảnh CCCD (căn cước công dân) mặt trước/sau để xác minh danh tính (eKYC). Admin sẽ xét duyệt đơn; nếu được chấp nhận, tài khoản sẽ có thêm vai trò Nhân viên và có thể chuyển đổi qua lại giữa chế độ khách hàng và nhân viên.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000010",
            "Chính sách hủy của nhân viên và tạm ngưng tài khoản",
            "Sau khi đã nhận việc, nhân viên vẫn có thể hủy kèm lý do; đơn sẽ được đăng lại để tìm nhân viên khác, khách hàng được thông báo. Tuy nhiên nếu một nhân viên hủy quá 3 lần trong 30 ngày, tài khoản sẽ tự động bị tạm ngưng và chỉ admin mới có thể kích hoạt lại.",
            now);
    }

    private static KnowledgeDocument CreateKnowledgeDocument(string id, string title, string content, DateTime now) => new()
    {
        Id = Guid.Parse(id),
        Title = title,
        Content = content,
        Source = "Development seed",
        IsActive = true,
        CreatedAt = now
    };
}
