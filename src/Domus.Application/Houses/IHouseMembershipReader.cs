namespace Domus.Application.Houses;

public interface IHouseMembershipReader
{
    Task<IReadOnlyList<HouseMembershipSummary>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
