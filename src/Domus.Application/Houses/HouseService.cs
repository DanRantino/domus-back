using Domus.Application.Common;
using Domus.Application.Users;

namespace Domus.Application.Houses;

public sealed class HouseService(IUserStore userStore, IHouseMembershipReader membershipReader)
{
    public async Task<AppResult<IReadOnlyList<HouseMembershipSummary>>> ListMineAsync(
        string identityId,
        CancellationToken cancellationToken)
    {
        var user = await userStore.FindByIdentityIdAsync(identityId, cancellationToken);
        if (user is null)
        {
            return AppResult<IReadOnlyList<HouseMembershipSummary>>.Failure(
                ErrorCodes.NotProvisioned,
                "User is not provisioned");
        }

        var houses = await membershipReader.ListByUserIdAsync(user.Id, cancellationToken);
        return AppResult<IReadOnlyList<HouseMembershipSummary>>.Success(houses);
    }

    public async Task<AppResult<HouseMembershipSummary>> GetMineAsync(
        string identityId,
        Guid houseId,
        CancellationToken cancellationToken)
    {
        var listResult = await ListMineAsync(identityId, cancellationToken);
        if (!listResult.IsSuccess)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                listResult.Error!.Code,
                listResult.Error.Message);
        }

        var house = listResult.Value!.FirstOrDefault(item => item.Id == houseId);
        if (house is null)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                ErrorCodes.NotFound,
                "House not found");
        }

        return AppResult<HouseMembershipSummary>.Success(house);
    }
}
