using System.Net;
using System.Text;
using System.Text.Json;
using Commonword.Tests.Integration.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Commonword.Tests.Integration.Telemetry;

[Collection("Api")]
public class TelemetryEndpointsTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public TelemetryEndpointsTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync("telemetry_events");
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostEvent_MissingClient_Returns400()
    {
        var response = await PostEventAsync(new
        {
            client = "",
            playerId = "player1",
            type = "puzzle_started",
            payload = new { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEvent_MissingPlayerId_Returns400()
    {
        var response = await PostEventAsync(new
        {
            client = "android",
            playerId = "",
            type = "puzzle_started",
            payload = new { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEvent_Valid_Returns201WithId()
    {
        var response = await PostEventAsync(new
        {
            client = "android",
            playerId = "player1",
            type = "puzzle_started",
            payload = new { puzzleId = "abc" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(response);
        body.TryGetProperty("id", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PostEvent_WithSessionId_Returns201()
    {
        var response = await PostEventAsync(new
        {
            client = "android",
            playerId = "player1",
            type = "cell_entered",
            payload = new { row = 0, col = 0 },
            sessionId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private Task<HttpResponseMessage> PostEventAsync(object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        return _client.PostAsync("/telemetry/events", content);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
