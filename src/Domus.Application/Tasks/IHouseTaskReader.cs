using Domus.Domain.Tasks;

namespace Domus.Application.Tasks;

public interface IHouseTaskReader
{
    Task<IReadOnlyList<HouseTaskSummary>> ListSanctuaryByHouseIdsAsync(
        IReadOnlyList<Guid> houseIds,
        CancellationToken cancellationToken);

    Task<HouseTask?> FindByIdAsync(
        Guid houseId,
        Guid taskId,
        CancellationToken cancellationToken);

    Task<HouseTaskSummary?> GetByIdAsync(
        Guid houseId,
        Guid taskId,
        CancellationToken cancellationToken);

    Task<bool> TryCompletePendingAsync(
        Guid houseId,
        Guid taskId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
