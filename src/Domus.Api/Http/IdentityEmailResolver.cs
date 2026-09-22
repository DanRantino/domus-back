using System.Net.Http.Headers;
using System.Text.Json;
using Logto.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Domus.Api.Http;

public sealed class IdentityEmailResolver(
    IHttpClientFactory httpClientFactory,
    IOptions<IdentityEmailOptions> options)
{
    public async Task<string?> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var fromClaims = context.User.GetEmail();
        if (!string.IsNullOrWhiteSpace(fromClaims))
        {
            return fromClaims;
        }

        var accessToken = await ResolveAccessTokenAsync(context);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var authority = options.Value.Authority?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authority))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{authority}/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var client = httpClientFactory.CreateClient(nameof(IdentityEmailResolver));
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("email", out var email)
                && email.ValueKind == JsonValueKind.String)
            {
                return email.GetString();
            }

            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    private static async Task<string?> ResolveAccessTokenAsync(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var bearer = authorization["Bearer ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(bearer))
            {
                return bearer;
            }
        }

        var cookieToken = await context.GetTokenAsync(
            LogtoDefaults.CookieScheme,
            "access_token");
        return string.IsNullOrWhiteSpace(cookieToken) ? null : cookieToken;
    }
}

public sealed class IdentityEmailOptions
{
    public string? Authority { get; set; }
}
