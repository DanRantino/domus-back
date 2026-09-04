namespace Domus.Api.Contracts.Houses;

public sealed record CreateInvitationRequest(string? Email, string? Role);
