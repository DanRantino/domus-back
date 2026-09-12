using Domus.Application.Tasks;

namespace Domus.Api.Contracts.Tasks;

public sealed record HouseTaskMemberResponse(Guid UserId, string? DisplayName)
{
    public static HouseTaskMemberResponse FromApplication(HouseTaskMemberSummary summary) =>
        new(summary.UserId, summary.DisplayName);
}

public sealed record HouseTaskResponse(
    Guid Id,
    Guid HouseId,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    HouseTaskMemberResponse? Assignee,
    HouseTaskMemberResponse CreatedBy)
{
    public static HouseTaskResponse FromApplication(HouseTaskSummary summary) =>
        new(
            summary.Id,
            summary.HouseId,
            summary.Title,
            summary.Description,
            summary.Status,
            summary.DueAt,
            summary.CompletedAt,
            summary.Assignee is null
                ? null
                : HouseTaskMemberResponse.FromApplication(summary.Assignee),
            HouseTaskMemberResponse.FromApplication(summary.CreatedBy));
}
