using Domus.Domain.Houses;

namespace Domus.Domain.Tasks;

public sealed class HouseTask
{
    public Guid Id { get; private set; }

    public Guid HouseId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string Status { get; private set; } = null!;

    public DateTimeOffset? DueAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public Guid? AssigneeUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public House? House { get; private set; }

    public HouseMembership CreatedByMembership { get; private set; } = null!;

    public HouseMembership? AssigneeMembership { get; private set; }

    private HouseTask()
    {
    }

    public HouseTask(
        Guid id,
        Guid houseId,
        string title,
        Guid createdByUserId,
        DateTimeOffset now,
        string? description = null,
        DateTimeOffset? dueAt = null,
        Guid? assigneeUserId = null)
    {
        Id = id;
        HouseId = houseId;
        Title = title;
        Description = description;
        Status = HouseTaskStatuses.Pending;
        DueAt = dueAt;
        CompletedAt = null;
        AssigneeUserId = assigneeUserId;
        CreatedByUserId = createdByUserId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        Status = HouseTaskStatuses.Completed;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void Reopen(DateTimeOffset now)
    {
        Status = HouseTaskStatuses.Pending;
        CompletedAt = null;
        UpdatedAt = now;
    }

    public void AssignTo(Guid assigneeUserId, DateTimeOffset now)
    {
        AssigneeUserId = assigneeUserId;
        UpdatedAt = now;
    }

    public void Unassign(DateTimeOffset now)
    {
        AssigneeUserId = null;
        UpdatedAt = now;
    }

    public void ChangeDueDate(DateTimeOffset? dueAt, DateTimeOffset now)
    {
        DueAt = dueAt;
        UpdatedAt = now;
    }
}
