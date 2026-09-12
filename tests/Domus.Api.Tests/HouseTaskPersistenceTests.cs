using Domus.Api.Tests.Support;
using Domus.Application.Tasks;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests;

public sealed class HouseTaskPersistenceTests : IAsyncLifetime
{
    private readonly DomusApiFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HouseTask_PersistsWithHouseAndAssignee()
    {
        var creator = await _factory.SeedUserAsync("identity-task-creator", "Ana");
        var assignee = await _factory.SeedUserAsync("identity-task-assignee", "Bruno");
        var house = await _factory.SeedHouseWithMembershipAsync(
            creator.Id,
            "Casa Centro",
            HouseRoles.Admin);
        await _factory.SeedMembershipAsync(assignee.Id, house.Id, HouseRoles.Member);

        var dueAt = DateTimeOffset.Parse("2026-09-05T18:00:00Z");
        var created = await _factory.SeedHouseTaskAsync(
            house.Id,
            creator.Id,
            "Comprar ração",
            assignee.Id,
            "Ração do cachorro",
            dueAt: dueAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var loaded = await db.HouseTasks.AsNoTracking().SingleAsync(task => task.Id == created.Id);

        Assert.Equal(house.Id, loaded.HouseId);
        Assert.Equal("Comprar ração", loaded.Title);
        Assert.Equal("Ração do cachorro", loaded.Description);
        Assert.Equal(HouseTaskStatuses.Pending, loaded.Status);
        Assert.Equal(dueAt, loaded.DueAt);
        Assert.Equal(assignee.Id, loaded.AssigneeUserId);
        Assert.Equal(creator.Id, loaded.CreatedByUserId);
        Assert.Null(loaded.CompletedAt);
    }

    [Fact]
    public async Task TryCompletePending_SecondWriter_DoesNotReplaceCompletedAt()
    {
        var user = await _factory.SeedUserAsync("identity-task-complete-race");
        var house = await _factory.SeedHouseWithMembershipAsync(
            user.Id,
            "Casa Centro",
            HouseRoles.Admin);
        var task = await _factory.SeedHouseTaskAsync(house.Id, user.Id, "Comprar ração");
        var firstCompletedAt = DateTimeOffset.Parse("2026-09-12T15:00:00Z");
        var secondCompletedAt = DateTimeOffset.Parse("2026-09-12T15:00:01Z");

        using var firstScope = _factory.Services.CreateScope();
        using var secondScope = _factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IHouseTaskReader>();
        var second = secondScope.ServiceProvider.GetRequiredService<IHouseTaskReader>();

        var firstWon = await first.TryCompletePendingAsync(
            house.Id,
            task.Id,
            firstCompletedAt,
            CancellationToken.None);
        var secondWon = await second.TryCompletePendingAsync(
            house.Id,
            task.Id,
            secondCompletedAt,
            CancellationToken.None);

        Assert.True(firstWon);
        Assert.False(secondWon);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var loaded = await db.HouseTasks.AsNoTracking().SingleAsync(item => item.Id == task.Id);
        Assert.Equal(HouseTaskStatuses.Completed, loaded.Status);
        Assert.Equal(firstCompletedAt, loaded.CompletedAt);
        Assert.Equal(firstCompletedAt, loaded.UpdatedAt);
    }

    [Fact]
    public async Task TryCompletePending_ConcurrentWriters_OnlyOneSetsTimestamp()
    {
        var user = await _factory.SeedUserAsync("identity-task-complete-concurrent");
        var house = await _factory.SeedHouseWithMembershipAsync(
            user.Id,
            "Casa Centro",
            HouseRoles.Admin);
        var task = await _factory.SeedHouseTaskAsync(house.Id, user.Id, "Comprar ração");
        var firstCompletedAt = DateTimeOffset.Parse("2026-09-12T15:00:00Z");
        var secondCompletedAt = DateTimeOffset.Parse("2026-09-12T15:00:01Z");

        using var firstScope = _factory.Services.CreateScope();
        using var secondScope = _factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IHouseTaskReader>();
        var second = secondScope.ServiceProvider.GetRequiredService<IHouseTaskReader>();

        var firstWonTask = first.TryCompletePendingAsync(
            house.Id,
            task.Id,
            firstCompletedAt,
            CancellationToken.None);
        var secondWonTask = second.TryCompletePendingAsync(
            house.Id,
            task.Id,
            secondCompletedAt,
            CancellationToken.None);
        var firstWon = await firstWonTask;
        var secondWon = await secondWonTask;

        var winners = new[] { firstWon, secondWon }.Count(won => won);
        Assert.Equal(1, winners);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var loaded = await db.HouseTasks.AsNoTracking().SingleAsync(item => item.Id == task.Id);
        var expected = firstWon ? firstCompletedAt : secondCompletedAt;
        Assert.Equal(HouseTaskStatuses.Completed, loaded.Status);
        Assert.Equal(expected, loaded.CompletedAt);
        Assert.Equal(expected, loaded.UpdatedAt);
    }
}
