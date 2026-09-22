using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;
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
              tasks {
                id
                houseId
                title
                description
                status
                dueAt
                completedAt
                assignee {
                  userId
                  displayName
                }
                createdBy {
                  userId
                  displayName
                }
              }
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
        Assert.Empty(membership.Tasks);
    }

    [Fact]
    public async Task Me_WithAccessibleHouseTasks_ReturnsSanctuaryTasks()
    {
        const string identityId = "identity-gql-tasks";
        var creator = await _factory.SeedUserAsync(identityId, "Ana Admin");
        var assignee = await _factory.SeedUserAsync("identity-gql-assignee", "Bruno Member");
        var house = await _factory.SeedHouseWithMembershipAsync(
            creator.Id,
            "Casa Centro",
            HouseRoles.Admin);
        await _factory.SeedMembershipAsync(assignee.Id, house.Id, HouseRoles.Member);

        var dueAt = DateTimeOffset.UtcNow.Date;
        var pending = await _factory.SeedHouseTaskAsync(
            house.Id,
            creator.Id,
            "Comprar ração",
            assignee.Id,
            "Ração do cachorro",
            dueAt: dueAt);
        var unassigned = await _factory.SeedHouseTaskAsync(
            house.Id,
            creator.Id,
            "Limpar a cozinha");
        var completed = await _factory.SeedHouseTaskAsync(
            house.Id,
            creator.Id,
            "Trocar roupa de cama",
            assignee.Id,
            status: HouseTaskStatuses.Completed,
            completedAt: DateTimeOffset.UtcNow.AddHours(-1));
        await _factory.SeedHouseTaskAsync(
            house.Id,
            creator.Id,
            "Tarefa antiga",
            status: HouseTaskStatuses.Completed,
            completedAt: DateTimeOffset.UtcNow.AddDays(-30));

        var client = _factory.CreateAuthenticatedClient(identityId);
        var response = await PostMeQuery(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GraphqlEnvelope>(_jsonOptions);
        Assert.NotNull(body?.Data?.Me);
        var membership = Assert.Single(body.Data.Me.Houses);
        Assert.Equal(3, membership.Tasks.Count);

        var titles = membership.Tasks.Select(task => task.Title).ToHashSet();
        Assert.Contains("Comprar ração", titles);
        Assert.Contains("Limpar a cozinha", titles);
        Assert.Contains("Trocar roupa de cama", titles);
        Assert.DoesNotContain("Tarefa antiga", titles);

        var assigned = Assert.Single(membership.Tasks, task => task.Id == pending.Id.ToString());
        Assert.Equal(house.Id.ToString(), assigned.HouseId);
        Assert.Equal("pending", assigned.Status);
        Assert.Equal(assignee.Id.ToString(), assigned.Assignee?.UserId);
        Assert.Equal("Bruno Member", assigned.Assignee?.DisplayName);
        Assert.Equal(creator.Id.ToString(), assigned.CreatedBy.UserId);
        Assert.Equal("Ana Admin", assigned.CreatedBy.DisplayName);

        var open = Assert.Single(membership.Tasks, task => task.Id == unassigned.Id.ToString());
        Assert.Null(open.Assignee);

        var done = Assert.Single(membership.Tasks, task => task.Id == completed.Id.ToString());
        Assert.Equal("completed", done.Status);
        Assert.NotNull(done.CompletedAt);
    }

    [Fact]
    public async Task Me_DoesNotReturnTasksFromInaccessibleHouses()
    {
        const string identityId = "identity-gql-own-house";
        var user = await _factory.SeedUserAsync(identityId, "Ana");
        var other = await _factory.SeedUserAsync("identity-gql-other", "Carlos");
        var ownHouse = await _factory.SeedHouseWithMembershipAsync(
            user.Id,
            "Casa da Ana",
            HouseRoles.Admin);
        var otherHouse = await _factory.SeedHouseWithMembershipAsync(
            other.Id,
            "Casa do Carlos",
            HouseRoles.Admin);

        await _factory.SeedHouseTaskAsync(ownHouse.Id, user.Id, "Tarefa visível");
        await _factory.SeedHouseTaskAsync(otherHouse.Id, other.Id, "Tarefa secreta");

        var client = _factory.CreateAuthenticatedClient(identityId);
        var response = await PostMeQuery(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GraphqlEnvelope>(_jsonOptions);
        Assert.NotNull(body?.Data?.Me);
        var membership = Assert.Single(body.Data.Me.Houses);
        Assert.Equal(ownHouse.Id.ToString(), membership.Id);
        var task = Assert.Single(membership.Tasks);
        Assert.Equal("Tarefa visível", task.Title);
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

    private sealed record GraphqlHouse(
        string Id,
        string Name,
        string Role,
        IReadOnlyList<GraphqlHouseTask> Tasks);

    private sealed record GraphqlHouseTask(
        string Id,
        string HouseId,
        string Title,
        string? Description,
        string Status,
        DateTimeOffset? DueAt,
        DateTimeOffset? CompletedAt,
        GraphqlHouseTaskMember? Assignee,
        GraphqlHouseTaskMember CreatedBy);

    private sealed record GraphqlHouseTaskMember(string UserId, string? DisplayName);

    private sealed record GraphqlError(string Message, GraphqlErrorExtensions? Extensions);

    private sealed record GraphqlErrorExtensions(string? Code);
}
