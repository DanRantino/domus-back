using System.Security.Claims;
using Domus.Api.Contracts.Users;
using Domus.Api.Http;
using Domus.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController(MeService meService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(ApiEnvelope<MeResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<MeResponse>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<MeResponse>>> GetMe(
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<MeResponse>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var result = await meService.GetAsync(
            currentUser.Id,
            currentUser.FullName,
            currentUser.NotifyDailyTasks,
            currentUser.NotifyExpenses,
            currentUser.NotifyFamilyChat,
            currentUser.Theme,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(MeResponse.FromApplication));
    }

    [HttpPost("me")]
    [ProducesResponseType(
        typeof(ApiEnvelope<MeResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<MeResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<MeResponse>>> Provision(
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequireIdentity<MeResponse>(
            HttpContext,
            out var identityId,
            out var failure))
        {
            return failure;
        }

        var result = await meService.ProvisionAsync(
            identityId,
            HttpContext.User.FindFirstValue("name"),
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(MeResponse.FromApplication),
            createdAt: result.IsCreated ? "/users/me" : null);
    }
}
