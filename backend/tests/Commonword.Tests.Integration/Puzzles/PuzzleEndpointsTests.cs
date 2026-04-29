using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Commonword.Tests.Integration.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Commonword.Tests.Integration.Puzzles;

[Collection("Api")]
public class PuzzleEndpointsTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public PuzzleEndpointsTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync("puzzles");
    public Task DisposeAsync() => Task.CompletedTask;

    // GET /puzzles/today

    [Fact]
    public async Task GetToday_NoPuzzles_Returns404()
    {
        var response = await _client.GetAsync("/puzzles/today");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetToday_NoDaily_ReturnsMostRecentImport()
    {
        var id = await ImportPuzzleAsync();

        var response = await _client.GetAsync("/puzzles/today");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("id").GetString().Should().Be(id.ToString());
    }

    [Fact]
    public async Task GetToday_DailySet_ReturnsMarkedPuzzle()
    {
        await ImportPuzzleAsync();
        var dailyId = await ImportPuzzleAsync();
        await MarkDailyAsync(dailyId);

        var response = await _client.GetAsync("/puzzles/today");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("id").GetString().Should().Be(dailyId.ToString());
        body.GetProperty("isDaily").GetBoolean().Should().BeTrue();
    }

    // GET /puzzles/{id}

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/puzzles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsCorrectData()
    {
        var id = await ImportPuzzleAsync();

        var response = await _client.GetAsync($"/puzzles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("id").GetString().Should().Be(id.ToString());
        body.GetProperty("title").GetString().Should().Be("Test Puzzle");
    }

    // POST /admin/puzzles/import/ipuz

    [Fact]
    public async Task ImportIpuz_NoAdminKey_Returns401()
    {
        var response = await _client.PostAsync(
            "/admin/puzzles/import/ipuz",
            JsonContent(TestData.SimpleIpuz));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportIpuz_WrongAdminKey_Returns403()
    {
        var response = await _client.PostAsync(
            "/admin/puzzles/import/ipuz",
            JsonContent(TestData.SimpleIpuz),
            adminKey: "wrong-key");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ImportIpuz_InvalidIpuz_Returns400()
    {
        var response = await _client.PostAsync(
            "/admin/puzzles/import/ipuz",
            JsonContent(TestData.InvalidIpuz),
            adminKey: TestData.AdminKey);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportIpuz_ValidIpuz_Returns201WithPuzzle()
    {
        var response = await _client.PostAsync(
            "/admin/puzzles/import/ipuz",
            JsonContent(TestData.SimpleIpuz),
            adminKey: TestData.AdminKey);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(response);
        body.GetProperty("title").GetString().Should().Be("Test Puzzle");
        body.TryGetProperty("id", out _).Should().BeTrue();
    }

    // POST /admin/puzzles/{id}/mark-daily

    [Fact]
    public async Task MarkDaily_NoAdminKey_Returns401()
    {
        var id = await ImportPuzzleAsync();

        var response = await _client.PostAsync($"/admin/puzzles/{id}/mark-daily", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkDaily_UnknownId_Returns404()
    {
        var response = await _client.PostAsync(
            $"/admin/puzzles/{Guid.NewGuid()}/mark-daily",
            content: null,
            adminKey: TestData.AdminKey);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkDaily_ValidId_Returns204AndSetsDaily()
    {
        var firstId = await ImportPuzzleAsync();
        await MarkDailyAsync(firstId);
        var secondId = await ImportPuzzleAsync();

        await MarkDailyAsync(secondId);

        var today = await ReadJsonAsync(await _client.GetAsync("/puzzles/today"));
        today.GetProperty("id").GetString().Should().Be(secondId.ToString());

        var first = await ReadJsonAsync(await _client.GetAsync($"/puzzles/{firstId}"));
        first.GetProperty("isDaily").GetBoolean().Should().BeFalse();
    }

    // Helpers

    private async Task<Guid> ImportPuzzleAsync()
    {
        var response = await _client.PostAsync(
            "/admin/puzzles/import/ipuz",
            JsonContent(TestData.SimpleIpuz),
            adminKey: TestData.AdminKey);

        response.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(response);
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private async Task MarkDailyAsync(Guid id)
    {
        var response = await _client.PostAsync(
            $"/admin/puzzles/{id}/mark-daily",
            content: null,
            adminKey: TestData.AdminKey);

        response.EnsureSuccessStatusCode();
    }

    private static StringContent JsonContent(string json)
        => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}

file static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PostAsync(
        this HttpClient client,
        string url,
        HttpContent? content,
        string adminKey = "")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        if (!string.IsNullOrEmpty(adminKey))
        {
            request.Headers.Add("X-Admin-Key", adminKey);
        }

        return client.SendAsync(request);
    }
}
