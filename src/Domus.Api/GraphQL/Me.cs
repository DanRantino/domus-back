using Domus.Application.Houses;
using Domus.Application.Users;
using HotChocolate.Types.Relay;

namespace Domus.Api.GraphQL;

public sealed record Me(
    [property: ID] Guid Id,
    string? Name,
    UserProfile Profile,
    IReadOnlyList<MeHouse> Houses)
{
    public static Me FromApplication(MeResult result) =>
        new(
            result.Id,
            string.IsNullOrWhiteSpace(result.FullName) ? null : result.FullName,
            new UserProfile(
                result.Theme,
                result.NotifyDailyTasks,
                result.NotifyExpenses,
                result.NotifyFamilyChat),
            result.Houses.Select(MeHouse.FromApplication).ToArray());
}

public sealed record UserProfile(
    string Theme,
    bool NotifyDailyTasks,
    bool NotifyExpenses,
    bool NotifyFamilyChat);

public sealed record MeHouse(
    [property: ID] Guid Id,
    string Name,
    string Role)
{
    public static MeHouse FromApplication(HouseMembershipSummary summary) =>
        new(summary.Id, summary.Name, summary.Role);
}
