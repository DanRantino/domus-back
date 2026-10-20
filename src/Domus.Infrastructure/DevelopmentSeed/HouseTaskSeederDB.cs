using Domus.Domain.Houses;
using Domus.Domain.Tasks;
using Domus.Domain.Users;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class HouseTaskSeederDB
{
    private readonly DomusDbContext _dbContext;

    public HouseTaskSeederDB(DomusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RunAsync(
        IReadOnlyList<House> houses,
        IReadOnlyList<SeededUser> users,
        CancellationToken cancellationToken = default)
    {
        var usersByEmail = users.ToDictionary(
            user => user.primaryEmail,
            StringComparer.OrdinalIgnoreCase);

        var housesByName = houses.ToDictionary(house => house.Name);
        var now = DateTimeOffset.UtcNow;
        var catalog = SeedHouseTasks.GetTasks();

        await RemoveTasksMissingFromCatalogAsync(houses, catalog, cancellationToken);

        foreach (var seedTask in catalog)
        {
            if (!housesByName.TryGetValue(seedTask.HouseName, out var house))
            {
                throw new InvalidOperationException(
                    $"Seed task '{seedTask.Title}' references unknown house '{seedTask.HouseName}'.");
            }

            var createdByUser = await ResolveDbUserAsync(
                seedTask.CreatedByEmail,
                usersByEmail,
                seedTask.Title,
                cancellationToken);

            Guid? assigneeUserId = null;
            if (seedTask.AssigneeEmail is not null)
            {
                var assignee = await ResolveDbUserAsync(
                    seedTask.AssigneeEmail,
                    usersByEmail,
                    seedTask.Title,
                    cancellationToken);
                assigneeUserId = assignee.Id;
            }

            var existing = await _dbContext.HouseTasks
                .FirstOrDefaultAsync(
                    task => task.HouseId == house.Id && task.Title == seedTask.Title,
                    cancellationToken);

            if (existing is not null)
            {
                if (assigneeUserId is Guid assigneeId && existing.AssigneeUserId != assigneeId)
                {
                    existing.AssignTo(assigneeId, now);
                }

                continue;
            }

            var dueAt = seedTask.DueFromNow is null
                ? (DateTimeOffset?)null
                : now + seedTask.DueFromNow.Value;

            var task = new HouseTask(
                Guid.NewGuid(),
                house.Id,
                seedTask.Title,
                createdByUser.Id,
                now,
                seedTask.Description,
                dueAt,
                assigneeUserId);

            if (seedTask.Status == HouseTaskStatuses.Completed)
            {
                var completedAt = seedTask.CompletedFromNow is null
                    ? now
                    : now + seedTask.CompletedFromNow.Value;
                task.Complete(completedAt);
            }

            _dbContext.HouseTasks.Add(task);
        }

        await _dbContext.SaveIfChangedAsync(cancellationToken);
    }

    private async Task RemoveTasksMissingFromCatalogAsync(
        IReadOnlyList<House> houses,
        IReadOnlyList<SeedHouseTask> catalog,
        CancellationToken cancellationToken)
    {
        var titlesByHouseName = catalog
            .GroupBy(task => task.HouseName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(task => task.Title).ToHashSet(StringComparer.Ordinal));

        foreach (var house in houses)
        {
            titlesByHouseName.TryGetValue(house.Name, out var titles);
            var existing = await _dbContext.HouseTasks
                .Where(task => task.HouseId == house.Id)
                .ToListAsync(cancellationToken);

            foreach (var task in existing)
            {
                if (titles is null || !titles.Contains(task.Title))
                {
                    _dbContext.HouseTasks.Remove(task);
                }
            }
        }
    }

    private async Task<User> ResolveDbUserAsync(
        string email,
        IReadOnlyDictionary<string, SeededUser> usersByEmail,
        string taskTitle,
        CancellationToken cancellationToken)
    {
        if (!usersByEmail.TryGetValue(email, out var seededUser))
        {
            throw new InvalidOperationException(
                $"Seed task '{taskTitle}' references unknown user email '{email}'.");
        }

        var dbUser = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.IdentityId == seededUser.id,
                cancellationToken);

        if (dbUser is null)
        {
            throw new InvalidOperationException(
                $"Seed task '{taskTitle}' could not resolve user '{email}' in the database.");
        }

        return dbUser;
    }
}
