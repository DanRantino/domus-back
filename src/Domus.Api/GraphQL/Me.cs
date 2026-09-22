using Domus.Application.Houses;
using Domus.Application.Tasks;
using Domus.Application.Users;
using HotChocolate.Types.Relay;

namespace Domus.Api.GraphQL;

public sealed record Me(
    [property: ID] Guid Id,
    string? Name,
    UserProfile Profile,
    IReadOnlyList<MeHouse> Houses)
{
    public static Me FromApplication(MeResult result)
    {
        var tasksByHouse = result.Tasks.ToLookup(task => task.HouseId);
        return new(
            result.Id,
            string.IsNullOrWhiteSpace(result.FullName) ? null : result.FullName,
            new UserProfile(
                result.Theme,
                result.NotifyDailyTasks,
                result.NotifyExpenses,
                result.NotifyFamilyChat),
            result.Houses
                .Select(house => MeHouse.FromApplication(house, tasksByHouse[house.Id]))
                .ToArray());
    }
}

public sealed record UserProfile(
    string Theme,
    bool NotifyDailyTasks,
    bool NotifyExpenses,
    bool NotifyFamilyChat);

public sealed record MeHouse(
    [property: ID] Guid Id,
    string Name,
    string Role,
    IReadOnlyList<MeHouseTask> Tasks)
{
    public static MeHouse FromApplication(
        HouseMembershipSummary summary,
        IEnumerable<HouseTaskSummary> tasks) =>
        new(
            summary.Id,
            summary.Name,
            summary.Role,
            tasks.Select(MeHouseTask.FromApplication).ToArray());
}

public sealed record MeHouseTask(
    [property: ID] Guid Id,
    [property: ID] Guid HouseId,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    MeHouseTaskMember? Assignee,
    MeHouseTaskMember CreatedBy)
{
    public static MeHouseTask FromApplication(HouseTaskSummary summary) =>
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
                : MeHouseTaskMember.FromApplication(summary.Assignee),
            MeHouseTaskMember.FromApplication(summary.CreatedBy));
}

public sealed record MeHouseTaskMember(
    [property: ID] Guid UserId,
    string? DisplayName)
{
    public static MeHouseTaskMember FromApplication(HouseTaskMemberSummary summary) =>
        new(summary.UserId, summary.DisplayName);
}
