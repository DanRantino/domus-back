using Domus.Api.Contracts.Houses;
using Domus.Api.Http;
using Domus.Application.Houses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("houses")]
[Authorize]
[Produces("application/json")]
public sealed class HousesController(HouseService houseService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiEnvelope<IReadOnlyList<HouseResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<IReadOnlyList<HouseResponse>>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<HouseResponse>>>> List(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetIdentityId(out var identityId))
        {
            return Unauthorized();
        }

        var result = await houseService.ListMineAsync(
            identityId,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(HouseResponse.FromApplication));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiEnvelope<HouseResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<HouseResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiEnvelope<HouseResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<HouseResponse>>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetIdentityId(out var identityId))
        {
            return Unauthorized();
        }

        var result = await houseService.GetMineAsync(
            identityId,
            id,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(HouseResponse.FromApplication));
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiEnvelope<HouseResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiEnvelope<HouseResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiEnvelope<HouseResponse>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiEnvelope<HouseResponse>>> Create(
        [FromBody] CreateHouseRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetIdentityId(out var identityId))
        {
            return Unauthorized();
        }

        var result = await houseService.CreateMineAsync(
            identityId,
            request?.Name,
            cancellationToken);

        return EnvelopeResults.ToActionResult(
            result.Map(HouseResponse.FromApplication),
            createdAt: result.IsSuccess ? $"/houses/{result.Value!.Id}" : null);
    }
}
