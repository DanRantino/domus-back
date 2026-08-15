using Domus.Domain.Houses;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed record SeedHouseMembership(
    string houseName,
    string email,
    string role);

public static class SeedHouseMemberships
{
    public static IReadOnlyList<SeedHouseMembership> GetMemberships() =>
    [
        new("Casa da Família", "dev1@domus.local", HouseRoles.Admin),
        new("Casa da Família", "dev2@domus.local", HouseRoles.Member),
        new("Casa da Família", "dev3@domus.local", HouseRoles.Member),
        new("Casa da Família", "dev4@domus.local", HouseRoles.Member),
        new("Casa do Admin", "dev1@domus.local", HouseRoles.Admin),
    ];
}
