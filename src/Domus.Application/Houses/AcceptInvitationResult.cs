namespace Domus.Application.Houses;

public sealed record AcceptInvitationResult(
    Guid HouseId,
    string HouseName,
    string Role);
