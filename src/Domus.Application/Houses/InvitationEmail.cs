namespace Domus.Application.Houses;

public sealed record InvitationEmail(
    string To,
    string HouseName,
    string? InviterName,
    string Token);
