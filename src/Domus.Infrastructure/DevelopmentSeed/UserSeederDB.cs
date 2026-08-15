using Domus.Infrastructure.DevelopmentSeed;
using Domus.Infrastructure.Identity;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Domus.Domain.Users;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class UserSeederDB
{
    private readonly DomusDbContext _dbContext;

    public UserSeederDB(DomusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SeededUser>> RunAsync(IReadOnlyList<SeededUser> usersToSeed,
        CancellationToken cancellationToken = default)
    {
        foreach (var user in usersToSeed)
        {
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == user.id, cancellationToken);
            if (existingUser is null)
            {
                var newUser = new User(
                    Guid.NewGuid(),
                    user.id,
                    user.name)
                {
                    NotifyDailyTasks = true,
                    NotifyExpenses = true,
                    NotifyFamilyChat = true,
                };
                _dbContext.Users.Add(newUser);
            }
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return usersToSeed;
    }
}
