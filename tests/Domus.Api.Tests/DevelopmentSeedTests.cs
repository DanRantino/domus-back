using Domus.Api.Tests.Support;
using Domus.Domain.Tasks;
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
        Assert.Equal(25, first.Tasks.Count);
        AssertEachMemberHasFiveTasks(first);

        await AddOutOfCatalogTaskAsync(first);
        await RenameUserAsync(first.Users[0].Id, "Nome antigo");
        await ReassignTaskAsync(
            first.Houses.Single(house => house.Name == "Casa da Família").Id,
            "Comprar ração",
            first.Users.Single(user => user.FullName == "Domus Admin").Id);

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

    private static void AssertEachMemberHasFiveTasks(SeedState state)
    {
        var familyId = state.Houses.Single(house => house.Name == "Casa da Família").Id;
        var adminId = state.Houses.Single(house => house.Name == "Casa do Admin").Id;
        var admin = state.Users.Single(user => user.FullName == "Domus Admin");

        Assert.Equal(20, state.Tasks.Count(task => task.HouseId == familyId));
        Assert.Equal(5, state.Tasks.Count(task => task.HouseId == adminId));
        Assert.Contains(state.Tasks, task =>
            task.HouseId == familyId &&
            task.Title == "Comprar ração" &&
            task.Status == "pending");
        Assert.Contains(state.Tasks, task =>
            task.HouseId == adminId &&
            task.Title == "Organizar os documentos" &&
            task.Status == "completed" &&
            task.AssigneeUserId == admin.Id);

        foreach (var user in state.Users)
        {
            var assigned = state.Tasks
                .Where(task => task.HouseId == familyId && task.AssigneeUserId == user.Id)
                .ToList();
            Assert.Equal(5, assigned.Count);
            Assert.Equal(1, assigned.Count(task => task.Status == "completed"));
            Assert.All(assigned, task => Assert.Equal(user.Id, task.CreatedByUserId));
        }
    }

    private async Task AddOutOfCatalogTaskAsync(SeedState state)
    {
        var houseId = state.Houses.Single(house => house.Name == "Casa da Família").Id;
        var userId = state.Users[0].Id;
        var now = DateTimeOffset.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        db.HouseTasks.Add(new HouseTask(
            Guid.NewGuid(),
            houseId,
            "Tarefa fora do catálogo",
            userId,
            now));
        await db.SaveChangesAsync();
    }

    private async Task ReassignTaskAsync(Guid houseId, string title, Guid assigneeUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var task = await db.HouseTasks.SingleAsync(row => row.HouseId == houseId && row.Title == title);
        task.AssignTo(assigneeUserId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    private async Task RenameUserAsync(Guid userId, string fullName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var user = await db.Users.SingleAsync(row => row.Id == userId);
        user.FullName = fullName;
        await db.SaveChangesAsync();
    }

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
