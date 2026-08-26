using Domus.Application.Houses;

namespace Domus.Api.Contracts.Houses;

public sealed record HouseResponse(
    Guid Id,
    string Name,
    string Role)
{
    public static HouseResponse FromApplication(HouseMembershipSummary summary) =>
        new(summary.Id, summary.Name, summary.Role);

    public static IReadOnlyList<HouseResponse> FromApplication(
        IReadOnlyList<HouseMembershipSummary> summaries) =>
        summaries.Select(FromApplication).ToArray();
}
