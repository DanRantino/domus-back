namespace Domus.Application.Tasks;

public sealed record HouseTaskMemberSummary(Guid UserId, string? DisplayName);

public sealed record HouseTaskSummary(
    Guid Id,
    Guid HouseId,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    HouseTaskMemberSummary? Assignee,
    HouseTaskMemberSummary CreatedBy);
