using Domus.Infrastructure.Identity;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class UserSeeder
{
    private readonly LogtoManagementClient _logtoManagementClient;

    public UserSeeder(LogtoManagementClient logtoManagementClient)
    {
        _logtoManagementClient = logtoManagementClient;
    }

    public async Task<IReadOnlyList<SeededUser>> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var seededUsers = new List<SeededUser>();
        var usersToSeed = SeedUsers.GetUsers();
        var users = await _logtoManagementClient.GetUsersAsync(cancellationToken);

        var usersToUpdate = users.Where(u => usersToSeed.Any(s =>
            s.email == u.primaryEmail && (
            s.username != u.username ||
            s.name != u.name))).ToList();

        foreach (var user in usersToUpdate)
        {
            var seedUser = usersToSeed.First(s => s.email == user.primaryEmail);
            var updatedUser = await _logtoManagementClient.UpdateUserAsync(user.id, new CreateLogtoUser(seedUser.email, seedUser.username, seedUser.name), cancellationToken);
            seededUsers.Add(updatedUser);
        }

        var usersToCreate = usersToSeed
            .Where(seedUser =>
                !users.Any(user =>
                    user.primaryEmail == seedUser.email))
            .ToList();

        foreach (var seedUser in usersToCreate)
        {
            var createdUser = await _logtoManagementClient.CreateUserAsync(
                new CreateLogtoUser(
                    seedUser.email,
                    seedUser.username,
                    seedUser.name),
                cancellationToken);
            seededUsers.Add(createdUser);
        }

        var existingUsers = users
            .Where(user =>
                usersToSeed.Any(seedUser => seedUser.email == user.primaryEmail) &&
                usersToUpdate.All(updated => updated.id != user.id))
            .Select(SeededUser.FromLogtoUser);

        seededUsers.AddRange(existingUsers);

        return seededUsers;
    }
}
