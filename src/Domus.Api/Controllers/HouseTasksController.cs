using Domus.Api.Contracts.Tasks;
using Domus.Api.Http;
using Domus.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("houses/{houseId:guid}/tasks")]
[Authorize]
[Produces("application/json")]
public sealed class HouseTasksController(HouseTaskService houseTaskService) : ControllerBase
{
    [HttpPost("{taskId:guid}/complete")]
    [ProducesResponseType(typeof(ApiEnvelope<HouseTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiEnvelope<HouseTaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiEnvelope<HouseTaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<HouseTaskResponse>>> Complete(
        Guid houseId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<HouseTaskResponse>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var result = await houseTaskService.CompleteAsync(
            currentUser.Id,
            houseId,
            taskId,
            cancellationToken);

        return EnvelopeResults.ToActionResult(result.Map(HouseTaskResponse.FromApplication));
    }
}
