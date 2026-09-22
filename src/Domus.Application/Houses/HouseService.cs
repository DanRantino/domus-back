using Domus.Application.Common;

namespace Domus.Application.Houses;

public sealed class HouseService(
    IHouseMembershipReader membershipReader,
    IHouseWriter houseWriter)
{
    public const int NameMaxLength = 256;

    public async Task<AppResult<IReadOnlyList<HouseMembershipSummary>>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var houses = await membershipReader.ListByUserIdAsync(userId, cancellationToken);
        return AppResult<IReadOnlyList<HouseMembershipSummary>>.Success(houses);
    }

    public async Task<AppResult<HouseMembershipSummary>> GetMineAsync(
        Guid userId,
        Guid houseId,
        CancellationToken cancellationToken)
    {
        var listResult = await ListMineAsync(userId, cancellationToken);
        var house = listResult.Value!.FirstOrDefault(item => item.Id == houseId);
        if (house is null)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                ErrorCodes.NotFound,
                "House not found");
        }

        return AppResult<HouseMembershipSummary>.Success(house);
    }

    public async Task<AppResult<HouseMembershipSummary>> CreateMineAsync(
        Guid userId,
        string? name,
        CancellationToken cancellationToken)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                ErrorCodes.ValidationError,
                "Name is required");
        }

        if (trimmed.Length > NameMaxLength)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                ErrorCodes.ValidationError,
                "Name is too long");
        }

        var house = await houseWriter.CreateWithOwnerAsync(
            userId,
            trimmed,
            cancellationToken);

        return AppResult<HouseMembershipSummary>.Created(house);
    }
}
