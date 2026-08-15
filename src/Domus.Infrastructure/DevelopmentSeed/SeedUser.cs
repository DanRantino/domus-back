namespace Domus.Infrastructure.Identity;

public sealed record SeedUser(
    string email,
    string name,
    string username
);
