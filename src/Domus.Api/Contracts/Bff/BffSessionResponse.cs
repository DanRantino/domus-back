namespace Domus.Api.Contracts.Bff;

public sealed record BffSessionResponse(
    bool Authenticated,
    string? Picture,
    string? Name,
    string? Username);
