using Domus.Application.Houses;
using Domus.Domain.Houses;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class HouseInvitationStore(DomusDbContext db) : IHouseInvitationStore
{
    public Task<HouseInvitation?> FindByIdAsync(
        Guid houseId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        return db.HouseInvitations
            .Include(i => i.House)
            .SingleOrDefaultAsync(
                i => i.Id == invitationId && i.HouseId == houseId,
                cancellationToken);
    }

    public Task<HouseInvitation?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return db.HouseInvitations
            .Include(i => i.House)
            .SingleOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }

    public Task<HouseInvitation?> FindPendingByHouseAndEmailAsync(
        Guid houseId,
        string email,
        CancellationToken cancellationToken)
    {
        return db.HouseInvitations.SingleOrDefaultAsync(
            i => i.HouseId == houseId
                && i.Email == email
                && i.Status == HouseInvitationStatuses.Pending,
            cancellationToken);
    }

    public async Task<IReadOnlyList<HouseInvitation>> ListPendingByHouseIdAsync(
        Guid houseId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var pending = await db.HouseInvitations
            .AsNoTracking()
            .Where(i =>
                i.HouseId == houseId
                && i.Status == HouseInvitationStatuses.Pending)
            .ToListAsync(cancellationToken);

        return pending
            .Where(i => i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .ToArray();
    }

    public async Task<int> CountPendingByHouseIdAsync(
        Guid houseId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var pending = await db.HouseInvitations
            .Where(i =>
                i.HouseId == houseId
                && i.Status == HouseInvitationStatuses.Pending)
            .Select(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);

        return pending.Count(expiresAt => expiresAt > now);
    }

    public async Task ExpireOverduePendingAsync(
        Guid houseId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var pending = await db.HouseInvitations
            .Where(i =>
                i.HouseId == houseId
                && i.Status == HouseInvitationStatuses.Pending)
            .ToListAsync(cancellationToken);

        var overdue = pending.Where(i => i.ExpiresAt <= now).ToList();

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var invitation in overdue)
        {
            invitation.Status = HouseInvitationStatuses.Expired;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task AddAsync(HouseInvitation invitation, CancellationToken cancellationToken)
    {
        db.HouseInvitations.Add(invitation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<bool> SaveChangesIgnoringUniqueViolationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
