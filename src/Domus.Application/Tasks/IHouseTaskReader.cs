namespace Domus.Application.Tasks;

public interface IHouseTaskReader
{
    Task<IReadOnlyList<HouseTaskSummary>> ListSanctuaryByHouseIdsAsync(
        IReadOnlyList<Guid> houseIds,
        CancellationToken cancellationToken);
}
