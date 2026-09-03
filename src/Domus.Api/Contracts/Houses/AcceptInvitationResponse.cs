using Domus.Application.Houses;

namespace Domus.Api.Contracts.Houses;

public sealed record AcceptInvitationResponse(
    Guid HouseId,
    string HouseName,
    string Role)
{
    public static AcceptInvitationResponse FromApplication(AcceptInvitationResult result) =>
        new(result.HouseId, result.HouseName, result.Role);
}
