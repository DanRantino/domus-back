using Domus.Application.Common;
using Domus.Application.Houses;
using Domus.Domain.Users;

namespace Domus.Application.Users;

public sealed class MeService(IUserStore userStore, IHouseMembershipReader membershipReader)
{
    public async Task<AppResult<MeResult>> GetAsync(
        string identityId,
        CancellationToken cancellationToken)
    {
        var user = await userStore.FindByIdentityIdAsync(identityId, cancellationToken);
        if (user is null)
        {
            return AppResult<MeResult>.Failure(
                ErrorCodes.NotProvisioned,
                "User is not provisioned");
        }

        var houses = await membershipReader.ListByUserIdAsync(user.Id, cancellationToken);
        return AppResult<MeResult>.Success(ToMeResult(user, houses));
    }

    public async Task<AppResult<MeResult>> ProvisionAsync(
        string identityId,
        CancellationToken cancellationToken)
    {
        var existing = await userStore.FindByIdentityIdAsync(identityId, cancellationToken);
        if (existing is not null)
        {
            return AppResult<MeResult>.Failure(
                ErrorCodes.AlreadyExists,
                "User already exists");
        }

        var user = new User(Guid.NewGuid(), identityId, fullName: null);
        await userStore.AddAsync(user, cancellationToken);
        var saved = await userStore.SaveChangesIgnoringUniqueViolationAsync(cancellationToken);
        if (!saved)
        {
            return AppResult<MeResult>.Failure(
                ErrorCodes.AlreadyExists,
                "User already exists");
        }

        return AppResult<MeResult>.Created(ToMeResult(user, []));
    }

    private static MeResult ToMeResult(
        User user,
        IReadOnlyList<HouseMembershipSummary> houses) =>
        new(
            user.Id,
            user.FullName!,
            user.NotifyDailyTasks,
            user.NotifyExpenses,
            user.NotifyFamilyChat,
            user.Theme,
            houses);
}
