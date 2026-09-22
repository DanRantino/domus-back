using Domus.Domain.Houses;

namespace Domus.Application.Houses;

public interface IHouseInvitationStore
{
    Task<HouseInvitation?> FindByIdAsync(
        Guid houseId,
        Guid invitationId,
        CancellationToken cancellationToken);

    Task<HouseInvitation?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<HouseInvitation?> FindPendingByHouseAndEmailAsync(
        Guid houseId,
        string email,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HouseInvitation>> ListPendingByHouseIdAsync(
        Guid houseId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<int> CountPendingByHouseIdAsync(
        Guid houseId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task ExpireOverduePendingAsync(
        Guid houseId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task AddAsync(HouseInvitation invitation, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> SaveChangesIgnoringUniqueViolationAsync(CancellationToken cancellationToken);
}
