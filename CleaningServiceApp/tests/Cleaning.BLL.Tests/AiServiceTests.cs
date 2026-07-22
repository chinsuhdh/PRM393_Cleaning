using System.Net;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Services;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed class AiServiceTests
{
    private sealed class SequencedHttpMessageHandler(params (HttpStatusCode StatusCode, string? Body)[] responses) : HttpMessageHandler
    {
        private int _index;

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));

            var (statusCode, body) = responses[Math.Min(_index, responses.Length - 1)];
            _index++;

            var response = new HttpResponseMessage(statusCode);
            if (body != null)
                response.Content = new StringContent(body);
            return response;
        }
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AiConfig:GroqApiKey"] = "test-key",
            ["AiConfig:GroqUrl"] = "https://groq.test/openai/v1/",
            ["AiConfig:GroqModel"] = "test-model"
        }).Build();

    private static AiService CreateService(AppDbContext context, SequencedHttpMessageHandler handler) =>
        new(context, new HttpClient(handler), CreateConfig(), NullLogger<AiService>.Instance);

    private static string ContentReply(string text) =>
        $$$"""{"choices":[{"message":{"role":"assistant","content":"{{{text}}}"},"finish_reason":"stop"}]}""";

    private static string ToolCallReply(string callId, string toolName, string argumentsJson) =>
        $$$"""{"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"{{{callId}}}","type":"function","function":{"name":"{{{toolName}}}","arguments":"{{{argumentsJson.Replace("\"", "\\\"")}}}"}}]},"finish_reason":"tool_calls"}]}""";

    [Fact(DisplayName = "[UT-AICHAT-01] Doc scoring picks the document matching the user's question")]
    public void SelectRelevantDocuments_PicksMatchingDocument()
    {
        var docs = new List<KnowledgeDocument>
        {
            new() { Id = Guid.NewGuid(), Title = "Cách đặt lịch", Content = "Khách hàng chọn dịch vụ và địa chỉ.", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Title = "Chính sách hủy đặt lịch", Content = "Hủy trước khi nhận việc thì miễn phí.", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var result = AiService.SelectRelevantDocuments(docs, "Tôi muốn hủy đặt lịch có mất phí không?", take: 1);

        Assert.Single(result);
        Assert.Contains("Hủy trước khi nhận việc", result[0]);
    }

    [Fact(DisplayName = "[UT-AICHAT-02] Doc scoring falls back to the most recent documents when nothing matches")]
    public void SelectRelevantDocuments_NoMatch_FallsBackToMostRecent()
    {
        var older = new KnowledgeDocument { Id = Guid.NewGuid(), Title = "A", Content = "một hai ba", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var newer = new KnowledgeDocument { Id = Guid.NewGuid(), Title = "B", Content = "bốn năm sáu", IsActive = true, CreatedAt = DateTime.UtcNow };

        var result = AiService.SelectRelevantDocuments([older, newer], "xyz không liên quan gì cả", take: 1);

        Assert.Single(result);
        Assert.Equal(newer.Content, result[0]);
    }

    [Fact(DisplayName = "[UT-AICHAT-03] A failed Groq call returns Success=false with the fallback reply")]
    public async Task ChatWithRagAsync_GroqFails_ReturnsFallbackWithSuccessFalse()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new SequencedHttpMessageHandler((HttpStatusCode.InternalServerError, null)));

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s1", Message = "Xin chào" });

        Assert.False(result.Success);
        Assert.NotEmpty(result.Reply);
        Assert.Empty(result.Suggestions);
    }

    [Fact(DisplayName = "[UT-AICHAT-04] A successful Groq call returns Success=true with the model's reply")]
    public async Task ChatWithRagAsync_GroqSucceeds_ReturnsSuccessTrue()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new SequencedHttpMessageHandler((HttpStatusCode.OK, ContentReply("Xin chào, tôi có thể giúp gì cho bạn?"))));

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s2", Message = "Xin chào" });

        Assert.True(result.Success);
        Assert.Equal("Xin chào, tôi có thể giúp gì cho bạn?", result.Reply);
    }

    [Fact(DisplayName = "[UT-AICHAT-05] History reflects both the user's message and the bot's reply, in order")]
    public async Task GetHistoryAsync_AfterChat_ReturnsBothMessagesInOrder()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new SequencedHttpMessageHandler((HttpStatusCode.OK, ContentReply("OK"))));
        var userId = Guid.NewGuid();

        var chatResult = await service.ChatWithRagAsync(userId, new ChatRequestDto { SessionId = "s3", Message = "Hỏi gì đó" });
        var history = await service.GetHistoryAsync(userId, chatResult.SessionId);

        Assert.Equal(2, history.Count);
        Assert.Equal("User", history[0].SenderType);
        Assert.Equal("Hỏi gì đó", history[0].Message);
        Assert.Equal("Ai", history[1].SenderType);
        Assert.Equal("OK", history[1].Message);
    }

    [Fact(DisplayName = "[UT-AICHAT-06] Clearing history removes the conversation entirely")]
    public async Task ClearHistoryAsync_RemovesConversationAndMessages()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new SequencedHttpMessageHandler((HttpStatusCode.OK, ContentReply("OK"))));
        var userId = Guid.NewGuid();
        var chatResult = await service.ChatWithRagAsync(userId, new ChatRequestDto { SessionId = "s4", Message = "Hỏi gì đó" });

        await service.ClearHistoryAsync(userId, chatResult.SessionId);
        var history = await service.GetHistoryAsync(userId, chatResult.SessionId);

        Assert.Empty(history);
    }

    [Fact(DisplayName = "[UT-AICHAT-07] A tool call round-trip sends the tool result back with the matching tool_call_id")]
    public async Task ChatWithRagAsync_ToolCall_SendsToolResultBack()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var serviceEntity = new Service { Id = Guid.NewGuid(), Name = "Dọn nhà theo giờ", IsActive = true, BasePrice = 100000, MinimumHours = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Services.Add(serviceEntity);
        context.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            ClientId = userId,
            ServiceId = serviceEntity.Id,
            Service = serviceEntity,
            Status = BookingStatus.Accepted,
            TotalPrice = 200000,
            ScheduledStartTime = DateTime.UtcNow.AddDays(1),
            ScheduledEndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            ClientId = otherUserId,
            ServiceId = serviceEntity.Id,
            Service = serviceEntity,
            Status = BookingStatus.Accepted,
            TotalPrice = 999999,
            ScheduledStartTime = DateTime.UtcNow.AddDays(2),
            ScheduledEndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new SequencedHttpMessageHandler(
            (HttpStatusCode.OK, ToolCallReply("call_1", "get_my_bookings", "{}")),
            (HttpStatusCode.OK, ContentReply("Bạn có 1 đơn sắp tới.")));
        var service = CreateService(context, handler);

        var result = await service.ChatWithRagAsync(userId, new ChatRequestDto { SessionId = "s5", Message = "Đơn của tôi thế nào?" });

        Assert.True(result.Success);
        Assert.Equal("Bạn có 1 đơn sắp tới.", result.Reply);
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Contains("\"tool_call_id\":\"call_1\"", handler.RequestBodies[1]);
        Assert.Contains("\"role\":\"tool\"", handler.RequestBodies[1]);
        Assert.Contains("200000", handler.RequestBodies[1]);
        Assert.Contains("Nhân viên đã nhận đơn", handler.RequestBodies[1]);
        Assert.DoesNotContain("999999", handler.RequestBodies[1]);
        Assert.Contains(result.Suggestions, s => s.Route == "/bookings");
    }

    [Fact(DisplayName = "[UT-AICHAT-08] Prior messages of the session are included as history, fallback replies filtered out")]
    public async Task ChatWithRagAsync_IncludesHistory_FiltersFallback()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var conversation = new AiConversation { Id = Guid.NewGuid(), UserId = userId, SessionId = "s6", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        context.AiConversations.Add(conversation);
        context.AiMessages.Add(new AiMessage { Id = Guid.NewGuid(), ConversationId = conversation.Id, SenderType = AiSenderType.User, Message = "Câu hỏi trước đó", CreatedAt = DateTime.UtcNow.AddMinutes(-9) });
        context.AiMessages.Add(new AiMessage { Id = Guid.NewGuid(), ConversationId = conversation.Id, SenderType = AiSenderType.Ai, Message = "Trả lời trước đó", CreatedAt = DateTime.UtcNow.AddMinutes(-8) });
        context.AiMessages.Add(new AiMessage { Id = Guid.NewGuid(), ConversationId = conversation.Id, SenderType = AiSenderType.Ai, Message = "Xin lỗi, hiện tại hệ thống AI đang quá tải. Quý khách vui lòng thử lại sau.", CreatedAt = DateTime.UtcNow.AddMinutes(-7) });
        await context.SaveChangesAsync();

        var handler = new SequencedHttpMessageHandler((HttpStatusCode.OK, ContentReply("OK")));
        var service = CreateService(context, handler);

        await service.ChatWithRagAsync(userId, new ChatRequestDto { SessionId = "s6", Message = "Câu hỏi mới" });

        Assert.Single(handler.RequestBodies);
        Assert.Contains("Câu hỏi trước đó", handler.RequestBodies[0]);
        Assert.Contains("Trả lời trước đó", handler.RequestBodies[0]);
        Assert.DoesNotContain("quá tải", handler.RequestBodies[0]);
    }

    [Fact(DisplayName = "[UT-AICHAT-10] Service detail tool returns options with surcharges and a deep-link suggestion")]
    public async Task ChatWithRagAsync_ServiceDetailTool_ReturnsOptionsAndDeepLink()
    {
        await using var context = CreateContext();
        var serviceEntity = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Dọn dẹp căn hộ",
            Description = "Dọn dẹp căn hộ chung cư",
            IsActive = true,
            BasePrice = 120000,
            MinimumHours = 2,
            BookingFormSchema = """{"questions":[{"id":"level","type":"single_choice","label":"Mức độ dọn dẹp","options":[{"id":"deep","label":"Dọn kỹ","priceDelta":50000,"durationDelta":30}]}]}""",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Services.Add(serviceEntity);
        await context.SaveChangesAsync();

        var handler = new SequencedHttpMessageHandler(
            (HttpStatusCode.OK, ToolCallReply("call_1", "get_service_detail", """{"serviceName":"căn hộ"}""")),
            (HttpStatusCode.OK, ContentReply("Dịch vụ dọn căn hộ giá 120.000đ/giờ.")));
        var service = CreateService(context, handler);

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s8", Message = "Cho tôi biết về dịch vụ dọn căn hộ" });

        Assert.True(result.Success);
        Assert.Contains("Dọn kỹ", handler.RequestBodies[1]);
        Assert.Contains("50000", handler.RequestBodies[1]);
        Assert.Contains(result.Suggestions, s => s.Route == $"/category/{serviceEntity.Id}");
    }

    [Fact(DisplayName = "[UT-AICHAT-11] Null tool arguments are treated as empty, not as a data error")]
    public async Task ChatWithRagAsync_NullToolArguments_ReturnsNoBookingsMessage()
    {
        await using var context = CreateContext();
        var handler = new SequencedHttpMessageHandler(
            (HttpStatusCode.OK, ToolCallReply("call_1", "get_my_bookings", "null")),
            (HttpStatusCode.OK, ContentReply("Bạn chưa có đơn đặt lịch nào.")));
        var service = CreateService(context, handler);

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s9", Message = "Đơn gần nhất của tôi thế nào?" });

        Assert.True(result.Success);
        Assert.Contains("chưa có đơn đặt lịch", handler.RequestBodies[1]);
        Assert.DoesNotContain("Không truy xuất được dữ liệu", handler.RequestBodies[1]);
    }

    [Fact(DisplayName = "[UT-AICHAT-09] The tool loop is bounded and the final iteration forces a text answer")]
    public async Task ChatWithRagAsync_ToolLoopBounded_LastIterationForcesText()
    {
        await using var context = CreateContext();
        var handler = new SequencedHttpMessageHandler(
            (HttpStatusCode.OK, ToolCallReply("call_a", "get_services", "{}")),
            (HttpStatusCode.OK, ToolCallReply("call_b", "get_services", "{}")),
            (HttpStatusCode.OK, ToolCallReply("call_c", "get_services", "{}")),
            (HttpStatusCode.OK, ToolCallReply("call_d", "get_services", "{}")));
        var service = CreateService(context, handler);

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s7", Message = "Có dịch vụ gì?" });

        Assert.False(result.Success);
        Assert.Equal(4, handler.RequestBodies.Count);
        Assert.Contains("\"tool_choice\":\"auto\"", handler.RequestBodies[0]);
        Assert.Contains("\"tool_choice\":\"none\"", handler.RequestBodies[3]);
    }
}
