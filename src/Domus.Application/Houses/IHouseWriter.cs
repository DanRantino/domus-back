namespace Domus.Application.Houses;

public interface IHouseWriter
{
    Task<HouseMembershipSummary> CreateWithOwnerAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken);
}
