using System.Linq.Expressions;
using Domus.Application.Tasks;
using Domus.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class HouseTaskReader(DomusDbContext db, TimeProvider timeProvider)
    : IHouseTaskReader
{
    public async Task<IReadOnlyList<HouseTaskSummary>> ListSanctuaryByHouseIdsAsync(
        IReadOnlyList<Guid> houseIds,
        CancellationToken cancellationToken)
    {
        if (houseIds.Count == 0)
        {
            return [];
        }

        var houseIdList = houseIds.Distinct().ToList();
        var inAccessibleHouses = HouseIdIsIn(houseIdList);
        var cutoff = timeProvider.GetUtcNow() - TimeSpan.FromDays(7);

        var pending = await QueryRows(
            db.HouseTasks
                .AsNoTracking()
                .Where(inAccessibleHouses)
                .Where(task => task.Status == HouseTaskStatuses.Pending),
            cancellationToken);

        var completed = await QueryRows(
            db.HouseTasks
                .AsNoTracking()
                .Where(inAccessibleHouses)
                .Where(task => task.Status == HouseTaskStatuses.Completed),
            cancellationToken);

        // Date filter stays in memory: EF SQLite cannot translate DateTimeOffset
        // comparisons, matching HouseInvitationStore.
        var rows = pending
            .Concat(completed.Where(row => row.CompletedAt >= cutoff))
            .ToList();
        if (rows.Count == 0)
        {
            return [];
        }

        var userIds = rows
            .SelectMany(row => row.AssigneeUserId is { } assignee
                ? new[] { row.CreatedByUserId, assignee }
                : new[] { row.CreatedByUserId })
            .Distinct()
            .ToList();

        var namesByUserId = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.FullName,
                cancellationToken);

        return rows
            .Select(row => new HouseTaskSummary(
                row.Id,
                row.HouseId,
                row.Title,
                row.Description,
                row.Status,
                row.DueAt,
                row.CompletedAt,
                row.AssigneeUserId is { } assigneeUserId
                    ? new HouseTaskMemberSummary(
                        assigneeUserId,
                        namesByUserId.GetValueOrDefault(assigneeUserId))
                    : null,
                new HouseTaskMemberSummary(
                    row.CreatedByUserId,
                    namesByUserId.GetValueOrDefault(row.CreatedByUserId))))
            .ToArray();
    }

    private static async Task<List<TaskRow>> QueryRows(
        IQueryable<HouseTask> query,
        CancellationToken cancellationToken) =>
        await query
            .Select(task => new TaskRow(
                task.Id,
                task.HouseId,
                task.Title,
                task.Description,
                task.Status,
                task.DueAt,
                task.CompletedAt,
                task.AssigneeUserId,
                task.CreatedByUserId))
            .ToListAsync(cancellationToken);

    private static Expression<Func<HouseTask, bool>> HouseIdIsIn(IReadOnlyList<Guid> houseIds)
    {
        var parameter = Expression.Parameter(typeof(HouseTask), "task");
        var property = Expression.Property(parameter, nameof(HouseTask.HouseId));
        Expression body = Expression.Equal(property, Expression.Constant(houseIds[0]));
        for (var index = 1; index < houseIds.Count; index++)
        {
            body = Expression.OrElse(
                body,
                Expression.Equal(property, Expression.Constant(houseIds[index])));
        }

        return Expression.Lambda<Func<HouseTask, bool>>(body, parameter);
    }

    private sealed record TaskRow(
        Guid Id,
        Guid HouseId,
        string Title,
        string? Description,
        string Status,
        DateTimeOffset? DueAt,
        DateTimeOffset? CompletedAt,
        Guid? AssigneeUserId,
        Guid CreatedByUserId);
}
