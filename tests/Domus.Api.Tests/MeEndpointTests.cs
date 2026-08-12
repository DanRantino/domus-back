using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Users;
using Domus.Api.Http;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;
using Domus.Domain.Users;
using Domus.Infrastructure.Persistence;
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

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_AuthenticatedButUnprovisioned_Returns403Envelope()
    {
        var client = CreateAuthenticatedClient("identity-unprovisioned");

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Null(body.Data);
        Assert.NotNull(body.Error);
        Assert.Equal("not_provisioned", body.Error.Code);
        Assert.Equal(0, await CountUsersAsync());
    }

    [Fact]
    public async Task GetMe_Provisioned_Returns200EnvelopeWithIdAndEmptyHouses()
    {
        const string identityId = "identity-provisioned";
        var user = await SeedUserAsync(identityId);
        var client = CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Null(body.Error);
        Assert.NotNull(body.Data);
        Assert.Equal(user.Id, body.Data.Id);
        Assert.Empty(body.Data.Houses);
    }

    [Fact]
    public async Task GetMe_ProvisionedWithMembership_ReturnsHouses()
    {
        const string identityId = "identity-with-house";
        var user = await SeedUserAsync(identityId);
        var house = await SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        var membership = Assert.Single(body.Data.Houses);
        Assert.Equal(house.Id, membership.Id);
        Assert.Equal("Casa Centro", membership.Name);
        Assert.Equal("admin", membership.Role);
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

        var user = new User(
            Guid.NewGuid(),
            identityId,
            null);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<House> SeedHouseWithMembershipAsync(Guid userId, string name, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var house = new House { Id = Guid.NewGuid(), Name = name };
        db.Houses.Add(house);
        db.HouseMemberships.Add(new HouseMembership
        {
            UserId = userId,
            HouseId = house.Id,
            Role = role,
        });
        await db.SaveChangesAsync();
        return house;
    }

    private Task<int> CountUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        return Task.FromResult(db.Users.Count());
    }
}
