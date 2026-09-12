using Domus.Application.Common;
using Domus.Application.Houses;
using Domus.Domain.Tasks;

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

        var task = await tasks.FindByIdAsync(houseId, taskId, cancellationToken);
        if (task is null)
        {
            return AppResult<HouseTaskSummary>.Failure(
                ErrorCodes.NotFound,
                "Task not found");
        }

        if (task.Status != HouseTaskStatuses.Completed)
        {
            task.Complete(timeProvider.GetUtcNow());
            await tasks.SaveChangesAsync(cancellationToken);
        }

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
