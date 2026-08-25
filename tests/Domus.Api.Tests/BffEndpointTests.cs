using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Bff;
using Domus.Api.Tests.Support;

namespace Domus.Api.Tests;

public sealed class BffEndpointTests : IAsyncLifetime
{
    private readonly DomusApiFactory _factory = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Session_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/bff/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_Authenticated_Returns200WithIdentityClaims()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, "identity-1");

        var response = await client.GetAsync("/bff/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BffSessionResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.True(body.Authenticated);
        Assert.Null(body.Picture);
    }

    [Fact]
    public async Task Login_ExternalReturnUrl_Returns400()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/bff/login?returnUrl=https://evil.example");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
