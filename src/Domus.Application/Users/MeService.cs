using Domus.Application.Common;
using Domus.Application.Houses;

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
        return AppResult<MeResult>.Success(new MeResult(user.Id, user.FullName, user.NotifyDailyTasks, user.NotifyExpenses, user.NotifyFamilyChat, user.Theme, houses));
    }
}
