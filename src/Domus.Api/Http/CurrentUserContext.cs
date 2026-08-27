using System.Diagnostics.CodeAnalysis;
using Domus.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domus.Api.Http;

public static class CurrentUserContext
{
    public const string ItemKey = nameof(CurrentUser);

    public static CurrentUser? Get(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value)
            ? value as CurrentUser
            : null;

    public static bool TryGet(
        HttpContext context,
        [NotNullWhen(true)] out CurrentUser user)
    {
        user = Get(context)!;
        return user is not null;
    }

    public static bool TryRequire<T>(
        HttpContext context,
        [NotNullWhen(true)] out CurrentUser user,
        [NotNullWhen(false)] out ActionResult<ApiEnvelope<T>>? failure)
    {
        if (TryGet(context, out user))
        {
            failure = null;
            return true;
        }

        if (context.User.Identity?.IsAuthenticated != true
            || !context.User.TryGetIdentityId(out _))
        {
            failure = new UnauthorizedResult();
            return false;
        }

        failure = EnvelopeResults.ToActionResult(
            AppResult<T>.Failure(
                ErrorCodes.NotProvisioned,
                "User is not provisioned"));
        return false;
    }
}
