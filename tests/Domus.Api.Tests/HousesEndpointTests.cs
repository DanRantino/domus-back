using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Houses;
using Domus.Api.Http;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;

namespace Domus.Api.Tests;

public sealed class HousesEndpointTests : IAsyncLifetime
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
    public async Task ListHouses_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/houses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListHouses_AuthenticatedButUnprovisioned_Returns403Envelope()
    {
        var client = _factory.CreateAuthenticatedClient("identity-unprovisioned");

        var response = await client.GetAsync("/houses");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<HouseResponse>>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Null(body.Data);
        Assert.NotNull(body.Error);
        Assert.Equal("not_provisioned", body.Error.Code);
        Assert.Equal(0, _factory.CountUsers());
    }

    [Fact]
    public async Task ListHouses_Provisioned_Returns200EmptyList()
    {
        const string identityId = "identity-provisioned";
        await _factory.SeedUserAsync(identityId);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync("/houses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<HouseResponse>>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Null(body.Error);
        Assert.NotNull(body.Data);
        Assert.Empty(body.Data);
    }

    [Fact]
    public async Task ListHouses_ProvisionedWithMembership_ReturnsHouse()
    {
        const string identityId = "identity-with-house";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync("/houses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<HouseResponse>>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        var membership = Assert.Single(body.Data);
        Assert.Equal(house.Id, membership.Id);
        Assert.Equal("Casa Centro", membership.Name);
        Assert.Equal("admin", membership.Role);
    }

    [Fact]
    public async Task GetHouse_Member_Returns200()
    {
        const string identityId = "identity-with-house";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync($"/houses/{house.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        Assert.Equal(house.Id, body.Data.Id);
        Assert.Equal("Casa Centro", body.Data.Name);
        Assert.Equal("admin", body.Data.Role);
    }

    [Fact]
    public async Task GetHouse_UnknownId_Returns404()
    {
        const string identityId = "identity-with-house";
        var user = await _factory.SeedUserAsync(identityId);
        await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.GetAsync($"/houses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Null(body.Data);
        Assert.NotNull(body.Error);
        Assert.Equal("not_found", body.Error.Code);
    }
}
