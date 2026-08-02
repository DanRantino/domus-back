using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Domus.Api.Features.Users;
using Domus.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests;

public sealed class MeEndpointTests : IAsyncLifetime
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
    public async Task GetMe_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_AuthenticatedButUnprovisioned_Returns403()
    {
        var client = CreateAuthenticatedClient("identity-unprovisioned");

        var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await CountUsersAsync());
    }

    [Fact]
    public async Task GetMe_Provisioned_Returns200WithUser()
    {
        const string identityId = "identity-provisioned";
        var user = await SeedUserAsync(identityId);
        var client = CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(user.Id, body.Id);
        Assert.Equal(identityId, body.IdentityId);
    }

    [Fact]
    public async Task PostMe_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/me", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostMe_FirstCall_Returns201AndPersistsUser()
    {
        const string identityId = "identity-new";
        var client = CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync("/me", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(identityId, body.IdentityId);
        Assert.NotEqual(Guid.Empty, body.Id);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var stored = Assert.Single(db.Users.Where(u => u.IdentityId == identityId).ToList());
        Assert.Equal(body.Id, stored.Id);
    }

    [Fact]
    public async Task PostMe_SecondCall_Returns409()
    {
        const string identityId = "identity-duplicate";
        var client = CreateAuthenticatedClient(identityId);

        var first = await client.PostAsync("/me", content: null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsync("/me", content: null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, await CountUsersAsync(identityId));
    }

    [Fact]
    public async Task PostMe_IgnoresForgedIdentityIdInBody()
    {
        const string tokenSub = "identity-from-token";
        var client = CreateAuthenticatedClient(tokenSub);
        using var content = new StringContent(
            """{"identity_id":"forged-identity"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/me", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(tokenSub, body.IdentityId);
        Assert.Equal(0, await CountUsersAsync("forged-identity"));
        Assert.Equal(1, await CountUsersAsync(tokenSub));
    }

    private HttpClient CreateAuthenticatedClient(string sub)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        return client;
    }

    private async Task<User> SeedUserAsync(string identityId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var user = new User { Id = Guid.NewGuid(), IdentityId = identityId };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private Task<int> CountUsersAsync(string? identityId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var count = identityId is null
            ? db.Users.Count()
            : db.Users.Count(u => u.IdentityId == identityId);
        return Task.FromResult(count);
    }
}
