using Domus.Application.Houses;

namespace Domus.Api.Contracts.Users;

public sealed record MeHouseResponse(
    Guid Id,
    string Name,
    string Role)
{
    public static MeHouseResponse FromApplication(HouseMembershipSummary summary) =>
        new(summary.Id, summary.Name, summary.Role);
}
