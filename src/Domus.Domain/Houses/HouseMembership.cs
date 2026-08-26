namespace Domus.Domain.Houses;

public sealed class HouseMembership
{
    public Guid UserId { get; set; }
    public Guid HouseId { get; set; }
    public required string Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public House? House { get; set; }
}
