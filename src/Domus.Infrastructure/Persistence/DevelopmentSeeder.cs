using Domus.Domain.Houses;
using Domus.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class DevelopmentSeeder(DomusDbContext db)
{

    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid HouseId = Guid.Parse("00000000-0000-0000-0000-000000000202");
    private const string IdentityId = "domus-local-user";
    private const string FullName = "Domus Local User";
    private const string HouseName = "Domus Local House";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == UserId, cancellationToken);

        if (user is null)
        {
            user = new User(UserId, IdentityId, FullName);
            db.Users.Add(user);
        }
        else
        {
            user.IdentityId = IdentityId;
            user.FullName = FullName;
        }

        var house = await db.Houses.SingleOrDefaultAsync(x => x.Name == HouseName, cancellationToken);

        if (house is null)
        {
            house = new House { Id = HouseId, Name = HouseName };
            db.Houses.Add(house);
        }
        else
        {
            house.Name = HouseName;
        }

        var membership = await db.HouseMemberships.SingleOrDefaultAsync(
            x => x.UserId == UserId && x.HouseId == HouseId,
            cancellationToken);

        if (membership is null)
        {
            membership = new HouseMembership
            {
                UserId = UserId,
                HouseId = HouseId,
                Role = HouseRoles.Admin,
            };
            db.HouseMemberships.Add(membership);
        }
        else
        {
            membership.Role = HouseRoles.Admin;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
