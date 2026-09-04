using Domus.Api.Tests.Support;
using Domus.Infrastructure.DevelopmentSeed;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests;

public sealed class DevelopmentSeedTests : IAsyncLifetime
{
    private readonly DomusApiFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DatabaseSeed_RunTwice_DoesNotChangeState()
    {
        var users = CreateSeededUsers();

        await SeedDatabaseAsync(users);
        var first = await CaptureStateAsync();

        Assert.Equal(4, first.Users.Count);
        Assert.Equal(2, first.Houses.Count);
        Assert.Equal(5, first.Memberships.Count);
        Assert.Equal(8, first.Tasks.Count);
        Assert.Contains(first.Tasks, task => task.Title == "Comprar ração" && task.Status == "pending");
        Assert.Contains(first.Tasks, task => task.Title == "Trocar roupa de cama" && task.Status == "completed");
        Assert.Contains(first.Tasks, task => task.Title == "Limpar a cozinha" && task.AssigneeUserId is null);
        Assert.Contains(first.Tasks, task => task.Title == "Revisar contas");
        Assert.Contains(first.Tasks, task => task.Title == "Organizar documentos" && task.Status == "completed");

        await SeedDatabaseAsync(users);
        var second = await CaptureStateAsync();

        Assert.Equal(first.Users, second.Users);
        Assert.Equal(first.Houses, second.Houses);
        Assert.Equal(first.Memberships, second.Memberships);
        Assert.Equal(first.Tasks, second.Tasks);
    }

    private static IReadOnlyList<SeededUser> CreateSeededUsers() =>
        SeedUsers.GetUsers()
            .Select(user => new SeededUser(
                id: $"seed-{user.email}",
                primaryEmail: user.email,
                name: user.name,
                username: user.username))
            .ToList();

    private async Task SeedDatabaseAsync(IReadOnlyList<SeededUser> users)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        await new UserSeederDB(db).RunAsync(users);
        var houses = await new HouseSeederDB(db).RunAsync();
        await new HouseMembershipSeederDB(db).RunAsync(houses, users);
        await new HouseTaskSeederDB(db).RunAsync(houses, users);
    }

    private async Task<SeedState> CaptureStateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();

        var users = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.IdentityId)
            .Select(user => new UserRow(user.Id, user.IdentityId, user.FullName))
            .ToListAsync();

        var houses = await db.Houses
            .AsNoTracking()
            .OrderBy(house => house.Name)
            .Select(house => new HouseRow(house.Id, house.Name))
            .ToListAsync();

        var memberships = await db.HouseMemberships
            .AsNoTracking()
            .OrderBy(membership => membership.HouseId)
            .ThenBy(membership => membership.UserId)
            .Select(membership => new MembershipRow(
                membership.HouseId,
                membership.UserId,
                membership.Role))
            .ToListAsync();

        var tasks = await db.HouseTasks
            .AsNoTracking()
            .OrderBy(task => task.HouseId)
            .ThenBy(task => task.Title)
            .Select(task => new TaskRow(
                task.HouseId,
                task.Title,
                task.Status,
                task.AssigneeUserId,
                task.CreatedByUserId))
            .ToListAsync();

        return new SeedState(users, houses, memberships, tasks);
    }

    private sealed record UserRow(Guid Id, string IdentityId, string? FullName);

    private sealed record HouseRow(Guid Id, string Name);

    private sealed record MembershipRow(Guid HouseId, Guid UserId, string Role);

    private sealed record TaskRow(
        Guid HouseId,
        string Title,
        string Status,
        Guid? AssigneeUserId,
        Guid CreatedByUserId);

    private sealed record SeedState(
        IReadOnlyList<UserRow> Users,
        IReadOnlyList<HouseRow> Houses,
        IReadOnlyList<MembershipRow> Memberships,
        IReadOnlyList<TaskRow> Tasks);
}
