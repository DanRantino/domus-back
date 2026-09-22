using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Auth;
using Domus.Api.Tests.Support;
using Logto.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    [Fact]
    public void LogtoCookie_UsesNoneSameSiteAndAlwaysSecure()
    {
        var options = _factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(LogtoDefaults.CookieScheme);

        Assert.Equal(SameSiteMode.None, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.True(options.Cookie.HttpOnly);
    }

    [Fact]
    public void LogtoCookieAuth_DoesNotBindResourceTokenRefresh()
    {
        var options = _factory.Services
            .GetRequiredService<IOptionsMonitor<LogtoOptions>>()
            .Get(LogtoDefaults.AuthenticationScheme);

        Assert.True(string.IsNullOrEmpty(options.Resource));
        Assert.Contains(LogtoParameters.Scopes.Email, options.Scopes);
        Assert.True(options.GetClaimsFromUserInfoEndpoint);
    }
}
