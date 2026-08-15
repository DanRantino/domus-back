namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class AppSeed(
    UserSeeder userSeederIdentity,
    UserSeederDB userSeederDB,
    HouseSeederDB houseSeederDB,
    HouseMembershipSeederDB houseMembershipSeederDB)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var users = await userSeederIdentity.RunAsync(cancellationToken);
        Console.WriteLine($"Users created on Logto: {string.Join(", ", users)}");
        var usersDB = await userSeederDB.RunAsync(users, cancellationToken);
        Console.WriteLine($"Users created on DB: {string.Join(", ", usersDB)}");
        var houses = await houseSeederDB.RunAsync(cancellationToken);
        Console.WriteLine($"Houses created on DB: {string.Join(", ", houses.Select(h => h.Name))}");
        await houseMembershipSeederDB.RunAsync(houses, usersDB, cancellationToken);
        Console.WriteLine("House memberships seeded.");
    }
}
