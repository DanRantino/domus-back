namespace Domus.Domain.Houses;

public sealed class HouseInvitation
{
    public Guid Id { get; set; }

    public Guid HouseId { get; set; }

    public Guid InvitedByUserId { get; set; }

    public required string Email { get; set; }

    public required string Role { get; set; }

    public required string TokenHash { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public House? House { get; set; }
}
