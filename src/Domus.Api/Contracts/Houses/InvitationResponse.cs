using Domus.Application.Houses;

namespace Domus.Api.Contracts.Houses;

public sealed record InvitationResponse(
    Guid Id,
    Guid HouseId,
    string Email,
    string Role,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    string? Token,
    bool? EmailSent)
{
    public static InvitationResponse FromApplication(HouseInvitationSummary summary) =>
        new(
            summary.Id,
            summary.HouseId,
            summary.Email,
            summary.Role,
            summary.Status,
            summary.ExpiresAt,
            summary.CreatedAt,
            summary.Token,
            summary.EmailSent);
}
