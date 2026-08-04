using Domus.Domain.Users;

namespace Domus.Application.Users;

public interface IUserStore
{
    Task<User?> FindByIdentityIdAsync(string identityId, CancellationToken cancellationToken);

    Task<User?> FindTrackedByIdentityIdAsync(string identityId, CancellationToken cancellationToken);

    Task<User> AddAsync(User user, CancellationToken cancellationToken);

    Task<bool> SaveChangesIgnoringUniqueViolationAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
