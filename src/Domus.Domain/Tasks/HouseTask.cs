namespace Domus.Domain.Tasks;

public sealed class HouseTask
{
    public Guid Id { get; set; }
    public Guid HouseId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = HouseTaskStatuses.Pending;
    public DateTimeOffset? DueAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
