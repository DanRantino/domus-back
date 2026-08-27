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
        if (!User.TryGetIdentityId(out var identityId))
        {
            return Unauthorized();
        }

        var result = await meService.GetAsync(
            identityId,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(MeResponse.FromApplication));
    }
}