using Domus.Infrastructure.Identity;

namespace Domus.Infrastructure.DevelopmentSeed;

public static class SeedUsers
{
    public static IReadOnlyList<SeedUser> GetUsers() =>
    [
        new SeedUser(
            "dev1@domus.local",
            "Domus Admin",
            "Domus_Admin1"),
        new SeedUser(
            "dev2@domus.local",
            "Domus Member",
            "Domus_Member1"),
        new SeedUser(
            "dev3@domus.local",
            "Domus Member",
            "Domus_Member2"),
        new SeedUser(
            "dev4@domus.local",
            "Domus Member",
            "Domus_Member3"),
    ];
}
