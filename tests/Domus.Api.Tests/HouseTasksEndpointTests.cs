using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Tasks;
using Domus.Api.Http;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests;

public sealed class HouseTasksEndpointTests : IAsyncLifetime
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
    public async Task CompleteTask_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/houses/{Guid.NewGuid()}/tasks/{Guid.NewGuid()}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompleteTask_AuthenticatedButUnprovisioned_Returns403Envelope()
    {
        var client = _factory.CreateAuthenticatedClient("identity-unprovisioned");

        var response = await client.PostAsync(
            $"/houses/{Guid.NewGuid()}/tasks/{Guid.NewGuid()}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("not_provisioned", body.Error!.Code);
    }

    [Fact]
    public async Task CompleteTask_Member_ReturnsCompletedTask()
    {
        const string identityId = "identity-task-member";
        var creator = await _factory.SeedUserAsync("identity-task-creator", "Ana Admin");
        var member = await _factory.SeedUserAsync(identityId, "Bruno Member");
        var house = await _factory.SeedHouseWithMembershipAsync(
            creator.Id,
            "Casa Centro",
            HouseRoles.Admin);
        await _factory.SeedMembershipAsync(member.Id, house.Id, HouseRoles.Member);
        var task = await _factory.SeedHouseTaskAsync(
            house.Id,
            creator.Id,
            "Comprar ração",
            member.Id,
            "Ração do cachorro");
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync(
            $"/houses/{house.Id}/tasks/{task.Id}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        Assert.True(body.Success);
        Assert.Equal(task.Id, body.Data.Id);
        Assert.Equal(house.Id, body.Data.HouseId);
        Assert.Equal("Comprar ração", body.Data.Title);
        Assert.Equal("Ração do cachorro", body.Data.Description);
        Assert.Equal(HouseTaskStatuses.Completed, body.Data.Status);
        Assert.NotNull(body.Data.CompletedAt);
        Assert.Equal(member.Id, body.Data.Assignee!.UserId);
        Assert.Equal("Bruno Member", body.Data.Assignee.DisplayName);
        Assert.Equal(creator.Id, body.Data.CreatedBy.UserId);
        Assert.Equal("Ana Admin", body.Data.CreatedBy.DisplayName);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var loaded = await db.HouseTasks.AsNoTracking().SingleAsync(item => item.Id == task.Id);
        Assert.Equal(HouseTaskStatuses.Completed, loaded.Status);
        Assert.NotNull(loaded.CompletedAt);
    }

    [Fact]
    public async Task CompleteTask_AlreadyCompleted_ReturnsCurrentTask()
    {
        const string identityId = "identity-task-done";
        var user = await _factory.SeedUserAsync(identityId, "Ana");
        var house = await _factory.SeedHouseWithMembershipAsync(
            user.Id,
            "Casa Centro",
            HouseRoles.Admin);
        var completedAt = DateTimeOffset.Parse("2026-09-12T15:00:00Z");
        var task = await _factory.SeedHouseTaskAsync(
            house.Id,
            user.Id,
            "Trocar roupa de cama",
            status: HouseTaskStatuses.Completed,
            completedAt: completedAt);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync(
            $"/houses/{house.Id}/tasks/{task.Id}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        Assert.Equal(HouseTaskStatuses.Completed, body.Data.Status);
        Assert.Equal(completedAt, body.Data.CompletedAt);
    }

    [Fact]
    public async Task CompleteTask_SecondRequest_KeepsOriginalCompletedAt()
    {
        const string identityId = "identity-task-overlap";
        var user = await _factory.SeedUserAsync(identityId, "Ana");
        var house = await _factory.SeedHouseWithMembershipAsync(
            user.Id,
            "Casa Centro",
            HouseRoles.Admin);
        var task = await _factory.SeedHouseTaskAsync(house.Id, user.Id, "Comprar ração");
        var client = _factory.CreateAuthenticatedClient(identityId);
        var url = $"/houses/{house.Id}/tasks/{task.Id}/complete";

        // One request at a time. Two in-flight clients against the shared-cache
        // SQLite fixture can deadlock inside SQLite and hang the test host.
        var firstResponse = await client.PostAsync(url, content: null);
        var secondResponse = await client.PostAsync(url, content: null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var firstBody = await firstResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        var secondBody = await secondResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        Assert.NotNull(firstBody?.Data?.CompletedAt);
        Assert.Equal(firstBody.Data.CompletedAt, secondBody?.Data?.CompletedAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var loaded = await db.HouseTasks.AsNoTracking().SingleAsync(item => item.Id == task.Id);
        Assert.Equal(HouseTaskStatuses.Completed, loaded.Status);
        Assert.Equal(firstBody.Data.CompletedAt, loaded.CompletedAt);
    }

    [Fact]
    public async Task CompleteTask_UnknownTask_Returns404()
    {
        const string identityId = "identity-task-missing";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(
            user.Id,
            "Casa Centro",
            HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync(
            $"/houses/{house.Id}/tasks/{Guid.NewGuid()}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("not_found", body.Error!.Code);
    }

    [Fact]
    public async Task CompleteTask_OtherHouse_Returns404()
    {
        const string identityId = "identity-task-outsider";
        var user = await _factory.SeedUserAsync(identityId);
        var other = await _factory.SeedUserAsync("identity-task-owner", "Carlos");
        await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa da Ana", HouseRoles.Admin);
        var otherHouse = await _factory.SeedHouseWithMembershipAsync(
            other.Id,
            "Casa do Carlos",
            HouseRoles.Admin);
        var task = await _factory.SeedHouseTaskAsync(otherHouse.Id, other.Id, "Tarefa secreta");
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsync(
            $"/houses/{otherHouse.Id}/tasks/{task.Id}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HouseTaskResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("not_found", body.Error!.Code);
        Assert.Equal("House not found", body.Error.Message);
    }
}
