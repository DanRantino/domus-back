using Domus.Application.Houses;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class HouseMembershipReader(DomusDbContext db) : IHouseMembershipReader
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
}
