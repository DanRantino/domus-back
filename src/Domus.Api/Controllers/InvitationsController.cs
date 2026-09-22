using Domus.Api.Contracts.Houses;
using Domus.Api.Http;
using Domus.Application.Houses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("invitations")]
[Produces("application/json")]
public sealed class InvitationsController(
    InvitationService invitationService,
    IdentityEmailResolver identityEmailResolver) : ControllerBase
{
    [HttpGet("preview")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationPreviewResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationPreviewResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<InvitationPreviewResponse>>> Preview(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        var result = await invitationService.PreviewAsync(token, cancellationToken);
        return EnvelopeResults.ToActionResult(
            result.Map(InvitationPreviewResponse.FromApplication));
    }

    [HttpPost("accept")]
    [Authorize]
    [ProducesResponseType(
        typeof(ApiEnvelope<AcceptInvitationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<AcceptInvitationResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiEnvelope<AcceptInvitationResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiEnvelope<AcceptInvitationResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<AcceptInvitationResponse>>> Accept(
        [FromBody] AcceptInvitationRequest? request,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<AcceptInvitationResponse>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var callerEmail = await identityEmailResolver.ResolveAsync(
            HttpContext,
            cancellationToken);

        var result = await invitationService.AcceptAsync(
            currentUser.Id,
            request?.Token,
            callerEmail,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(AcceptInvitationResponse.FromApplication));
    }
}
