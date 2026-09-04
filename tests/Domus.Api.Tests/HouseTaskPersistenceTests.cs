using Domus.Api.Tests.Support;
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
}
