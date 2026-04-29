using System.Net;
using System.Text;
using System.Text.Json;
using Commonword.Tests.Integration.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Commonword.Tests.Integration.Solving;

[Collection("Api")]
public class SolvingEndpointsTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;
    private Guid _puzzleId;

    public SolvingEndpointsTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync("entries", "solve_sessions", "puzzles");
        _puzzleId = await ImportPuzzleAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // POST /sessions

    [Fact]
    public async Task StartSession_UnknownPuzzle_Returns404()
    {
        var response = await PostJsonAsync("/sessions", new
        {
            puzzleId = Guid.NewGuid(),
            playerId = "player1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartSession_MissingPlayerId_Returns400()
    {
        var response = await PostJsonAsync("/sessions", new
        {
            puzzleId = _puzzleId,
            playerId = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartSession_Valid_Returns201WithSession()
    {
        var response = await PostJsonAsync("/sessions", new
        {
            puzzleId = _puzzleId,
            playerId = "player1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(response);
        body.GetProperty("puzzleId").GetString().Should().Be(_puzzleId.ToString());
        body.GetProperty("playerId").GetString().Should().Be("player1");
    }

    // -------------------------------------------------------------------------
    // GET /sessions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSession_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/sessions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSession_Valid_ReturnsSessionWithPuzzleAndEntries()
    {
        var sessionId = await StartSessionAsync("player1");

        var response = await _client.GetAsync($"/sessions/{sessionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("session").GetProperty("id").GetString().Should().Be(sessionId.ToString());
        body.TryGetProperty("puzzle", out _).Should().BeTrue();
        body.GetProperty("entries").GetArrayLength().Should().Be(0);
    }

    // PUT /sessions/{id}/cells/{row}/{col}

    [Fact]
    public async Task UpsertEntry_SetsValue_Returns200()
    {
        var sessionId = await StartSessionAsync("player1");

        var response = await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "A" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("value").GetString().Should().Be("A");
        body.GetProperty("row").GetInt32().Should().Be(0);
        body.GetProperty("col").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task UpsertEntry_UpdatesValue_Returns200WithNewValue()
    {
        var sessionId = await StartSessionAsync("player1");
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "A" });

        var response = await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "B" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("value").GetString().Should().Be("B");
    }

    [Fact]
    public async Task UpsertEntry_EmptyValue_ClearsEntry()
    {
        var sessionId = await StartSessionAsync("player1");
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "A" });

        var response = await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("value").GetString().Should().BeEmpty();
    }

    // POST /sessions/{id}/check-word

    [Fact]
    public async Task CheckWord_InvalidDirection_Returns400()
    {
        var sessionId = await StartSessionAsync("player1");

        var response = await PostJsonAsync($"/sessions/{sessionId}/check-word", new
        {
            direction = "diagonal",
            number = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckWord_IncompleteWord_ReturnsNotComplete()
    {
        var sessionId = await StartSessionAsync("player1");

        var response = await PostJsonAsync($"/sessions/{sessionId}/check-word", new
        {
            direction = "across",
            number = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("complete").GetBoolean().Should().BeFalse();
        body.GetProperty("correct").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CheckWord_CorrectWord_ReturnsCorrect()
    {
        var sessionId = await StartSessionAsync("player1");
        // Puzzle solution row 0 = "CAT" at across word 1 starting (0,0)
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "C" });
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/1", new { value = "A" });
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/2", new { value = "T" });

        var response = await PostJsonAsync($"/sessions/{sessionId}/check-word", new
        {
            direction = "across",
            number = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("complete").GetBoolean().Should().BeTrue();
        body.GetProperty("correct").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CheckWord_IncorrectWord_ReturnsIncorrectCells()
    {
        var sessionId = await StartSessionAsync("player1");
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/0", new { value = "X" });
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/1", new { value = "Y" });
        await PutJsonAsync($"/sessions/{sessionId}/cells/0/2", new { value = "Z" });

        var response = await PostJsonAsync($"/sessions/{sessionId}/check-word", new
        {
            direction = "across",
            number = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("complete").GetBoolean().Should().BeTrue();
        body.GetProperty("correct").GetBoolean().Should().BeFalse();
        body.GetProperty("incorrectCells").GetArrayLength().Should().Be(3);
    }

    // Helpers

    private async Task<Guid> ImportPuzzleAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/puzzles/import/ipuz")
        {
            Content = new StringContent(TestData.SimpleIpuz, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Admin-Key", TestData.AdminKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await ReadJsonAsync(response);
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private async Task<Guid> StartSessionAsync(string playerId)
    {
        var response = await PostJsonAsync("/sessions", new { puzzleId = _puzzleId, playerId });
        response.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(response);
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private Task<HttpResponseMessage> PostJsonAsync(string url, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        return _client.PostAsync(url, content);
    }

    private Task<HttpResponseMessage> PutJsonAsync(string url, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        return _client.PutAsync(url, content);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
