using Domus.Application.Common;
using Domus.Application.Houses;

namespace Domus.Application.Users;

public sealed class MeService(IHouseMembershipReader membershipReader)
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
            new MeResult(
                userId,
                fullName ?? string.Empty,
                notifyDailyTasks,
                notifyExpenses,
                notifyFamilyChat,
                theme,
                houses));
    }
}
