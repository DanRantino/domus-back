using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Auth;
using Domus.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Domus.Api.Tests;

public sealed class AuthEndpointTests : IAsyncLifetime
{
    private readonly DomusApiFactory _factory = new();
    private readonly JsonSerializerOptions _jsonOptions = EndpointTestData.SnakeCaseJson;

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetSession_Anonymous_ReturnsNotAuthenticated()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/auth/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthSessionResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Authenticated);
        Assert.Null(body.Picture);
        Assert.Null(body.Name);
    }

    [Fact]
    public async Task GetUsersMe_WithoutCredentials_Returns401WithoutRedirect()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
