using Domus.Infrastructure.Identity;

public sealed record SeededUser(
    string id,
    string primaryEmail,
    string name,
    string username)
{
    public static SeededUser FromLogtoUser(LogtoUser user) =>
        new(
            id: user.id,
            primaryEmail: user.primaryEmail ?? string.Empty,
            name: user.name ?? string.Empty,
            username: user.username ?? string.Empty);
}
