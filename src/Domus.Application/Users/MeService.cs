using Domus.Application.Common;
using Domus.Application.Houses;
using Domus.Domain.Users;

namespace Domus.Application.Users;

public sealed class MeService(
    IHouseMembershipReader membershipReader,
    IUserStore userStore)
{
    public async Task<AppResult<MeResult>> GetAsync(
        Guid userId,
        string? fullName,
        bool notifyDailyTasks,
        bool notifyExpenses,
        bool notifyFamilyChat,
        string theme,
        CancellationToken cancellationToken)
    {
        var houses = await membershipReader.ListByUserIdAsync(userId, cancellationToken);
        return AppResult<MeResult>.Success(
            ToResult(
                userId,
                fullName,
                notifyDailyTasks,
                notifyExpenses,
                notifyFamilyChat,
                theme,
                houses));
    }

    public async Task<AppResult<MeResult>> ProvisionAsync(
        string identityId,
        string? fullName,
        CancellationToken cancellationToken)
    {
        var existing = await userStore.FindByIdentityIdAsync(identityId, cancellationToken);
        if (existing is not null)
        {
            return AppResult<MeResult>.Failure(
                ErrorCodes.AlreadyExists,
                "User already exists");
        }

        var trimmedName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        var user = new User(Guid.NewGuid(), identityId, trimmedName);
        await userStore.AddAsync(user, cancellationToken);
        if (!await userStore.SaveChangesIgnoringUniqueViolationAsync(cancellationToken))
        {
            return AppResult<MeResult>.Failure(
                ErrorCodes.AlreadyExists,
                "User already exists");
        }

        return AppResult<MeResult>.Created(
            ToResult(
                user.Id,
                user.FullName,
                user.NotifyDailyTasks,
                user.NotifyExpenses,
                user.NotifyFamilyChat,
                user.Theme,
                []));
    }

    private static MeResult ToResult(
        Guid userId,
        string? fullName,
        bool notifyDailyTasks,
        bool notifyExpenses,
        bool notifyFamilyChat,
        string theme,
        IReadOnlyList<HouseMembershipSummary> houses) =>
        new(
            userId,
            fullName ?? string.Empty,
            notifyDailyTasks,
            notifyExpenses,
            notifyFamilyChat,
            theme,
            houses);
}
