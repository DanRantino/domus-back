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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
