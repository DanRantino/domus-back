namespace Domus.Infrastructure.DevelopmentSeed;

public static class SeedHouses
{
    public static IReadOnlyList<SeedHouse> GetHouses() =>
    [
        new("Casa da Família"),
        new("Casa do Admin"),
    ];
}
