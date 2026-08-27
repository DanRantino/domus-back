using Domus.Domain.Houses;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class HouseMembershipSeederDB
{
    private readonly DomusDbContext _dbContext;

    public HouseMembershipSeederDB(DomusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RunAsync(
        IReadOnlyList<House> houses,
        IReadOnlyList<SeededUser> users,
        CancellationToken cancellationToken = default)
    {
        var usersByEmail = users.ToDictionary(
            user => user.primaryEmail,
            StringComparer.OrdinalIgnoreCase);

        var housesByName = houses.ToDictionary(house => house.Name);

        foreach (var seedMembership in SeedHouseMemberships.GetMemberships())
        {
            if (!housesByName.TryGetValue(seedMembership.houseName, out var house))
            {
                throw new InvalidOperationException(
                    $"Seed membership references unknown house '{seedMembership.houseName}'.");
            }

            if (!usersByEmail.TryGetValue(seedMembership.email, out var seededUser))
            {
                throw new InvalidOperationException(
                    $"Seed house '{seedMembership.houseName}' references unknown user email '{seedMembership.email}'.");
            }

            var dbUser = await _dbContext.Users
                .FirstOrDefaultAsync(
                    u => u.IdentityId == seededUser.id,
                    cancellationToken);

            if (dbUser is null)
            {
                throw new InvalidOperationException(
                    $"Seed house '{seedMembership.houseName}' could not resolve user '{seedMembership.email}' in the database.");
            }

            var membership = await _dbContext.HouseMemberships
                .FirstOrDefaultAsync(
                    m => m.UserId == dbUser.Id && m.HouseId == house.Id,
                    cancellationToken);

            if (membership is null)
            {
                _dbContext.HouseMemberships.Add(new HouseMembership
                {
                    UserId = dbUser.Id,
                    HouseId = house.Id,
                    Role = seedMembership.role,
                });
            }
        }

        await _dbContext.SaveIfChangedAsync(cancellationToken);
    }
}
