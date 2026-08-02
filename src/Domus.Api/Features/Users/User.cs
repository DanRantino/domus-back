namespace Domus.Api.Features.Users;

public sealed class User
{
    public Guid Id { get; set; }

    public required string IdentityId { get; set; }
}
