using Domus.Application.Users;
using Domus.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class UserStore(DomusDbContext db) : IUserStore
{
    public Task<User?> FindByIdentityIdAsync(string identityId, CancellationToken cancellationToken)
    {
        return db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }

    public Task<User?> FindTrackedByIdentityIdAsync(string identityId, CancellationToken cancellationToken)
    {
        return db.Users
            .SingleOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        return Task.FromResult(user);
    }

    public async Task<bool> SaveChangesIgnoringUniqueViolationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
