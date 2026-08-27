namespace Domus.Api.Contracts.Auth;

public sealed record AuthSessionResponse(
    bool Authenticated,
    string? Picture,
    string? Name);
