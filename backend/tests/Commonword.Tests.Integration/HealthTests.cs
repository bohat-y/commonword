using System.Net;
using System.Text.Json;
using Commonword.Tests.Integration.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Commonword.Tests.Integration;

[Collection("Api")]
public class HealthTests(ApiFixture fixture)
{
    [Fact]
    public async Task GetHealth_Returns200WithOkStatus()
    {
        var response = await fixture.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("status").GetString().Should().Be("ok");
    }
}
