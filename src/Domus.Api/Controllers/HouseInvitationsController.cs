using Domus.Api.Contracts.Houses;
using Domus.Api.Http;
using Domus.Application.Houses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("houses/{houseId:guid}/invitations")]
[Authorize]
[Produces("application/json")]
public sealed class HouseInvitationsController(InvitationService invitationService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<InvitationResponse>>> Create(
        Guid houseId,
        [FromBody] CreateInvitationRequest? request,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<InvitationResponse>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var result = await invitationService.CreateAsync(
            currentUser.Id,
            currentUser.FullName,
            houseId,
            request?.Email,
            request?.Role,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(InvitationResponse.FromApplication),
            createdAt: result.IsSuccess
                ? $"/houses/{houseId}/invitations/{result.Value!.Id}"
                : null);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiEnvelope<IReadOnlyList<InvitationResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<IReadOnlyList<InvitationResponse>>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<InvitationResponse>>>> List(
        Guid houseId,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<IReadOnlyList<InvitationResponse>>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var result = await invitationService.ListPendingAsync(
            currentUser.Id,
            houseId,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(items =>
                (IReadOnlyList<InvitationResponse>)items
                    .Select(InvitationResponse.FromApplication)
                    .ToArray()));
    }

    [HttpDelete("{invitationId:guid}")]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<InvitationResponse>>> Revoke(
        Guid houseId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<InvitationResponse>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var result = await invitationService.RevokeAsync(
            currentUser.Id,
            houseId,
            invitationId,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(InvitationResponse.FromApplication));
    }

    [HttpPost("{invitationId:guid}/resend")]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiEnvelope<InvitationResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<InvitationResponse>>> Resend(
        Guid houseId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserContext.TryRequire<InvitationResponse>(
            HttpContext,
            out var currentUser,
            out var failure))
        {
            return failure;
        }

        var result = await invitationService.ResendAsync(
            currentUser.Id,
            currentUser.FullName,
            houseId,
            invitationId,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(InvitationResponse.FromApplication));
    }
}
