namespace Domus.Api.Http;

public static class LocalReturnUrl
{
    public static bool TryResolve(string? returnUrl, out string resolved)
    {
        resolved = "/";

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return true;
        }

        if (!returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            || returnUrl.Contains('\\', StringComparison.Ordinal)
            || returnUrl.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        resolved = returnUrl;
        return true;
    }
}
