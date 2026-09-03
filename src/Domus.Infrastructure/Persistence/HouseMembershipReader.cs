using Domus.Application.Houses;
using Domus.Domain.Houses;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class HouseMembershipReader(DomusDbContext db)
    : IHouseMembershipReader, IHouseWriter
{
    public async Task<IReadOnlyList<HouseMembershipSummary>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await db.HouseMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new HouseMembershipSummary(
                m.HouseId,
                m.House!.Name,
                m.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<HouseMembershipSummary> CreateWithOwnerAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken)
    {
        var house = new House
        {
            Id = Guid.NewGuid(),
            Name = name,
        };

        db.Houses.Add(house);
        db.HouseMemberships.Add(new HouseMembership
        {
            UserId = userId,
            HouseId = house.Id,
            Role = HouseRoles.Admin,
        });

        await db.SaveChangesAsync(cancellationToken);

        return new HouseMembershipSummary(house.Id, house.Name, HouseRoles.Admin);
    }

    public async Task AddMemberAsync(
        Guid userId,
        Guid houseId,
        string role,
        CancellationToken cancellationToken)
    {
        db.HouseMemberships.Add(new HouseMembership
        {
            UserId = userId,
            HouseId = houseId,
            Role = role,
        });

        await Task.CompletedTask;
    }
}
