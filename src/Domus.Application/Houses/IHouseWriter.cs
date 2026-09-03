namespace Domus.Application.Houses;

public interface IHouseWriter
{
    Task<HouseMembershipSummary> CreateWithOwnerAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken);

    Task AddMemberAsync(
        Guid userId,
        Guid houseId,
        string role,
        CancellationToken cancellationToken);
}
