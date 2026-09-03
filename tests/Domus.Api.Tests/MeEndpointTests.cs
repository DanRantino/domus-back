using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Users;
using Domus.Api.Http;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Domus.Api.Tests;

public sealed class MeEndpointTests : IAsyncLifetime
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
    public async Task GetMe_WithoutToken_Returns401()
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
    public async Task GetMe_AuthenticatedButUnprovisioned_Returns403Envelope()
    {
        var client = _factory.CreateAuthenticatedClient("identity-unprovisioned");

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Null(body.Data);
        Assert.NotNull(body.Error);
        Assert.Equal("not_provisioned", body.Error.Code);
        Assert.Equal(0, _factory.CountUsers());
    }

    [Fact]
    public async Task PostMe_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/users/me", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _factory.CountUsers());
    }

    [Fact]
    public async Task PostMe_Unprovisioned_Returns201AndPersists()
    {
        const string identityId = "identity-provision-me";
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync("/users/me", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/users/me", response.Headers.Location?.OriginalString);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        Assert.True(body.Success);
        Assert.Null(body.Error);
        Assert.Empty(body.Data.Houses);
        Assert.Equal("system", body.Data.Theme);
        Assert.True(body.Data.NotifyDailyTasks);
        Assert.True(body.Data.NotifyExpenses);
        Assert.True(body.Data.NotifyFamilyChat);
        Assert.Equal(1, _factory.CountUsers());

        var get = await client.GetAsync("/users/me");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var getBody = await get.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.Equal(body.Data.Id, getBody!.Data!.Id);
    }

    [Fact]
    public async Task PostMe_AlreadyProvisioned_Returns409()
    {
        const string identityId = "identity-already-provisioned";
        await _factory.SeedUserAsync(identityId);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync("/users/me", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Null(body.Data);
        Assert.NotNull(body.Error);
        Assert.Equal("already_exists", body.Error.Code);
        Assert.Equal(1, _factory.CountUsers());
    }

    [Fact]
    public async Task GetMe_Provisioned_Returns200EnvelopeWithIdAndEmptyHouses()
    {
        const string identityId = "identity-provisioned";
        var user = await _factory.SeedUserAsync(identityId);
        var client = _factory.CreateAuthenticatedClient(identityId);

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
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<MeResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        var membership = Assert.Single(body.Data.Houses);
        Assert.Equal(house.Id, membership.Id);
        Assert.Equal("Casa Centro", membership.Name);
        Assert.Equal("admin", membership.Role);
    }
}
