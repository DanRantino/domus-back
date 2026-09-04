using Domus.Domain.Tasks;

namespace Domus.Api.Tests;

public sealed class HouseTaskTests
{
    [Fact]
    public void Complete_SetsCompletedStatusAndTimestamp()
    {
        var createdAt = DateTimeOffset.Parse("2026-09-04T12:00:00Z");
        var completedAt = createdAt.AddHours(2);
        var task = CreatePendingTask(createdAt);

        task.Complete(completedAt);

        Assert.Equal(HouseTaskStatuses.Completed, task.Status);
        Assert.Equal(completedAt, task.CompletedAt);
        Assert.Equal(completedAt, task.UpdatedAt);
    }

    [Fact]
    public void Reopen_ResetsStatusAndClearsCompletedAt()
    {
        var createdAt = DateTimeOffset.Parse("2026-09-04T12:00:00Z");
        var completedAt = createdAt.AddHours(2);
        var reopenedAt = completedAt.AddMinutes(10);
        var task = CreatePendingTask(createdAt);
        task.Complete(completedAt);

        task.Reopen(reopenedAt);

        Assert.Equal(HouseTaskStatuses.Pending, task.Status);
        Assert.Null(task.CompletedAt);
        Assert.Equal(reopenedAt, task.UpdatedAt);
    }

    [Fact]
    public void AssignTo_And_Unassign_UpdateAssignee()
    {
        var now = DateTimeOffset.Parse("2026-09-04T12:00:00Z");
        var assigneeId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var task = CreatePendingTask(now);

        task.AssignTo(assigneeId, now.AddMinutes(1));
        Assert.Equal(assigneeId, task.AssigneeUserId);

        task.Unassign(now.AddMinutes(2));
        Assert.Null(task.AssigneeUserId);
    }

    private static HouseTask CreatePendingTask(DateTimeOffset now) =>
        new(
            Guid.Parse("00000000-0000-0000-0000-000000000301"),
            Guid.Parse("00000000-0000-0000-0000-000000000202"),
            "Comprar ração",
            Guid.Parse("00000000-0000-0000-0000-000000000101"),
            now);
}
