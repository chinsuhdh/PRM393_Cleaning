using System.Net;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Services;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed class AiServiceTests
{
    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string? responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (responseBody != null)
                response.Content = new StringContent(responseBody);
            return Task.FromResult(response);
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
            ["AiConfig:OllamaUrl"] = "http://localhost:11434",
            ["AiConfig:DefaultModel"] = "test-model"
        }).Build();

    private static AiService CreateService(AppDbContext context, HttpStatusCode statusCode, string? responseBody) =>
        new(context, new HttpClient(new FakeHttpMessageHandler(statusCode, responseBody)), CreateConfig(), NullLogger<AiService>.Instance);

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

    [Fact(DisplayName = "[UT-AICHAT-03] A failed Ollama call returns Success=false with the fallback reply")]
    public async Task ChatWithRagAsync_OllamaFails_ReturnsFallbackWithSuccessFalse()
    {
        await using var context = CreateContext();
        var service = CreateService(context, HttpStatusCode.InternalServerError, null);

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s1", Message = "Xin chào" });

        Assert.False(result.Success);
        Assert.NotEmpty(result.Reply);
    }

    [Fact(DisplayName = "[UT-AICHAT-04] A successful Ollama call returns Success=true with the model's reply")]
    public async Task ChatWithRagAsync_OllamaSucceeds_ReturnsSuccessTrue()
    {
        await using var context = CreateContext();
        var service = CreateService(context, HttpStatusCode.OK, "{\"response\":\"Xin chào, tôi có thể giúp gì cho bạn?\"}");

        var result = await service.ChatWithRagAsync(Guid.NewGuid(), new ChatRequestDto { SessionId = "s2", Message = "Xin chào" });

        Assert.True(result.Success);
        Assert.Equal("Xin chào, tôi có thể giúp gì cho bạn?", result.Reply);
    }

    [Fact(DisplayName = "[UT-AICHAT-05] History reflects both the user's message and the bot's reply, in order")]
    public async Task GetHistoryAsync_AfterChat_ReturnsBothMessagesInOrder()
    {
        await using var context = CreateContext();
        var service = CreateService(context, HttpStatusCode.OK, "{\"response\":\"OK\"}");
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
        var service = CreateService(context, HttpStatusCode.OK, "{\"response\":\"OK\"}");
        var userId = Guid.NewGuid();
        var chatResult = await service.ChatWithRagAsync(userId, new ChatRequestDto { SessionId = "s4", Message = "Hỏi gì đó" });

        await service.ClearHistoryAsync(userId, chatResult.SessionId);
        var history = await service.GetHistoryAsync(userId, chatResult.SessionId);

        Assert.Empty(history);
    }
}
