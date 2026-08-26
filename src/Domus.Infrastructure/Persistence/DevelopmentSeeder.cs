using Domus.Domain.Events;
using Domus.Domain.Expenses;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;
using Domus.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class DevelopmentSeeder(DomusDbContext db)
{
    private static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid MemberId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid HouseId = Guid.Parse("00000000-0000-0000-0000-000000000202");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = await EnsureUserAsync(AdminId, "domus-local-admin", "Domus Admin", cancellationToken);
        var member = await EnsureUserAsync(MemberId, "domus-local-member", "Domus Member", cancellationToken);

        var house = await db.Houses.SingleOrDefaultAsync(x => x.Id == HouseId, cancellationToken);
        if (house is null)
        {
            house = new House { Id = HouseId, Name = "Casa Domus" };
            db.Houses.Add(house);
        }

        await EnsureMembershipAsync(admin.Id, HouseRoles.Admin, cancellationToken);
        await EnsureMembershipAsync(member.Id, HouseRoles.Member, cancellationToken);

        if (!await db.HouseTasks.AnyAsync(x => x.HouseId == HouseId, cancellationToken))
        {
            db.HouseTasks.AddRange(
                new HouseTask { Id = Guid.Parse("00000000-0000-0000-0000-000000000301"), HouseId = HouseId, Title = "Comprar itens do mercado", AssignedToUserId = member.Id, CreatedByUserId = admin.Id, DueAt = DateTimeOffset.UtcNow.AddDays(1) },
                new HouseTask { Id = Guid.Parse("00000000-0000-0000-0000-000000000302"), HouseId = HouseId, Title = "Pagar conta de luz", CreatedByUserId = admin.Id, DueAt = DateTimeOffset.UtcNow.AddDays(3) });
        }

        if (!await db.Expenses.AnyAsync(x => x.HouseId == HouseId, cancellationToken))
        {
            db.Expenses.AddRange(
                new Expense { Id = Guid.Parse("00000000-0000-0000-0000-000000000401"), HouseId = HouseId, Description = "Mercado da semana", Amount = 287.45m, Category = "groceries", Date = DateOnly.FromDateTime(DateTime.UtcNow), PaidByUserId = admin.Id, CreatedByUserId = admin.Id },
                new Expense { Id = Guid.Parse("00000000-0000-0000-0000-000000000402"), HouseId = HouseId, Description = "Internet", Amount = 119.90m, Category = "utilities", Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), PaidByUserId = member.Id, CreatedByUserId = member.Id });
        }

        if (!await db.HouseEvents.AnyAsync(x => x.HouseId == HouseId, cancellationToken))
        {
            db.HouseEvents.Add(new HouseEvent { Id = Guid.Parse("00000000-0000-0000-0000-000000000501"), HouseId = HouseId, Title = "Jantar em família", Description = "Jantar de exemplo para desenvolvimento.", StartsAt = DateTimeOffset.UtcNow.AddDays(5).Date.AddHours(19), EndsAt = DateTimeOffset.UtcNow.AddDays(5).Date.AddHours(21), CreatedByUserId = admin.Id });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> EnsureUserAsync(Guid id, string identityId, string fullName, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            user = new User(id, identityId, fullName);
            db.Users.Add(user);
        }
        else
        {
            user.IdentityId = identityId;
            user.FullName = fullName;
        }

        return user;
    }

    private async Task EnsureMembershipAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var membership = await db.HouseMemberships.SingleOrDefaultAsync(x => x.UserId == userId && x.HouseId == HouseId, cancellationToken);
        if (membership is null)
        {
            db.HouseMemberships.Add(new HouseMembership { UserId = userId, HouseId = HouseId, Role = role });
        }
        else
        {
            membership.Role = role;
        }
    }
}
