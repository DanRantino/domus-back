using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Domus.Api.Http;

public static class HttpUser
{
    public static string? GetIdentityId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub");

    public static bool TryGetIdentityId(
        this ClaimsPrincipal user,
        [NotNullWhen(true)] out string identityId)
    {
        identityId = user.GetIdentityId() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(identityId);
    }
}

