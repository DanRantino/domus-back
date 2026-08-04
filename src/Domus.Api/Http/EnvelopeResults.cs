using Domus.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domus.Api.Http;

public static class EnvelopeResults
{
    public static ActionResult<ApiEnvelope<T>> ToActionResult<T>(
        AppResult<T> result,
        string? createdAt = null)
    {
        if (result.IsSuccess)
        {
            var envelope = ApiEnvelope<T>.Ok(result.Value!);
            return result.IsCreated
                ? new CreatedResult(createdAt ?? "/", envelope)
                : new OkObjectResult(envelope);
        }

        var error = result.Error!;
        var failure = ApiEnvelope<T>.Fail(error.Code, error.Message);
        var statusCode = error.Code switch
        {
            ErrorCodes.NotProvisioned => StatusCodes.Status403Forbidden,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.AlreadyExists => StatusCodes.Status409Conflict,
            ErrorCodes.ValidationError => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        return new ObjectResult(failure) { StatusCode = statusCode };
    }
}
