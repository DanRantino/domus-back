using System.Net;
using Domus.Api.Tests.Support;

namespace Domus.Api.Tests;

public sealed class HealthEndpointTests : IAsyncLifetime
{
    private readonly DomusApiFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password=", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=localhost;Database=unused", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"success\"", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthReady_WithDatabase_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Password=", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"success\"", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyHealth_IsRemoved()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
