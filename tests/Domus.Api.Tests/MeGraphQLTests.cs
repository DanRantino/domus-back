using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Domus.Api.Tests;

public sealed class MeGraphQLTests : IAsyncLifetime
{
    private const string MeQuery = """
        query {
          me {
            id
            name
            profile {
              theme
              notifyDailyTasks
              notifyExpenses
              notifyFamilyChat
            }
            houses {
              id
              name
              role
            }
          }
        }
        """;

    private readonly DomusApiFactory _factory = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await PostMeQuery(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Me_AuthenticatedButUnprovisioned_ReturnsNotProvisioned()
    {
        var client = _factory.CreateAuthenticatedClient("identity-unprovisioned");

        var response = await PostMeQuery(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GraphqlEnvelope>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Null(body.Data?.Me);
        var error = Assert.Single(body.Errors ?? []);
        Assert.Equal("not_provisioned", error.Extensions?.Code);
        Assert.Equal(0, _factory.CountUsers());
    }

    [Fact]
    public async Task Me_Provisioned_ReturnsProfileNameAndEmptyHouses()
    {
        const string identityId = "identity-gql-provisioned";
        var user = await _factory.SeedUserAsync(identityId);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await PostMeQuery(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GraphqlEnvelope>(_jsonOptions);
        Assert.NotNull(body?.Data?.Me);
        Assert.Null(body.Errors);
        Assert.Equal(user.Id.ToString(), body.Data.Me.Id);
        Assert.Null(body.Data.Me.Name);
        Assert.Equal("system", body.Data.Me.Profile.Theme);
        Assert.True(body.Data.Me.Profile.NotifyDailyTasks);
        Assert.True(body.Data.Me.Profile.NotifyExpenses);
        Assert.True(body.Data.Me.Profile.NotifyFamilyChat);
        Assert.Empty(body.Data.Me.Houses);
    }

    [Fact]
    public async Task Me_ProvisionedWithMembership_ReturnsHouses()
    {
        const string identityId = "identity-gql-house";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await PostMeQuery(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GraphqlEnvelope>(_jsonOptions);
        Assert.NotNull(body?.Data?.Me);
        var membership = Assert.Single(body.Data.Me.Houses);
        Assert.Equal(house.Id.ToString(), membership.Id);
        Assert.Equal("Casa Centro", membership.Name);
        Assert.Equal("admin", membership.Role);
    }

    private static Task<HttpResponseMessage> PostMeQuery(HttpClient client) =>
        client.PostAsJsonAsync(
            "/graphql",
            new { query = MeQuery },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private sealed record GraphqlEnvelope(
        GraphqlData? Data,
        IReadOnlyList<GraphqlError>? Errors);

    private sealed record GraphqlData(GraphqlMe? Me);

    private sealed record GraphqlMe(
        string Id,
        string? Name,
        GraphqlProfile Profile,
        IReadOnlyList<GraphqlHouse> Houses);

    private sealed record GraphqlProfile(
        string Theme,
        bool NotifyDailyTasks,
        bool NotifyExpenses,
        bool NotifyFamilyChat);

    private sealed record GraphqlHouse(string Id, string Name, string Role);

    private sealed record GraphqlError(string Message, GraphqlErrorExtensions? Extensions);

    private sealed record GraphqlErrorExtensions(string? Code);
}
