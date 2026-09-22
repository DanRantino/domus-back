using Domus.Application.Common;
using Domus.Application.Houses;

namespace Domus.Application.Tasks;

public sealed class HouseTaskService(
    IHouseMembershipReader memberships,
    IHouseTaskReader tasks,
    TimeProvider timeProvider)
{
    public async Task<AppResult<HouseTaskSummary>> CompleteAsync(
        Guid userId,
        Guid houseId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var houses = await memberships.ListByUserIdAsync(userId, cancellationToken);
        if (houses.All(house => house.Id != houseId))
        {
            return AppResult<HouseTaskSummary>.Failure(
                ErrorCodes.NotFound,
                "House not found");
        }

        await tasks.TryCompletePendingAsync(
            houseId,
            taskId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        var summary = await tasks.GetByIdAsync(houseId, taskId, cancellationToken);
        if (summary is null)
        {
            return AppResult<HouseTaskSummary>.Failure(
                ErrorCodes.NotFound,
                "Task not found");
        }

        return AppResult<HouseTaskSummary>.Success(summary);
    }
}
