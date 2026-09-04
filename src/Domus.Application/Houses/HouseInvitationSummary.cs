namespace Domus.Application.Houses;

public sealed record HouseInvitationSummary(
    Guid Id,
    Guid HouseId,
    string Email,
    string Role,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    string? Token = null,
    bool? EmailSent = null);
