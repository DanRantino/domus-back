using System.Text.Json;
using System.Text.Json.Serialization;
using Domus.Infrastructure.DevelopmentSeed;
using Microsoft.Extensions.Options;

namespace Domus.Infrastructure.Identity;

public sealed class LogtoManagementClient(
    HttpClient httpClient,
    IOptions<DevelopmentSeedOptions> options
)
{
    private readonly DevelopmentSeedOptions _options = options.Value;

    public async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_options.LogtoEndpoint)
            || string.IsNullOrWhiteSpace(_options.ManagementApiResource)
            || string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Missing required configuration: DevelopmentSeed ClientId, ClientSecret, LogtoEndpoint, and ManagementApiResource (env DevelopmentSeed__ClientId, DevelopmentSeed__ClientSecret, DevelopmentSeed__LogtoEndpoint, DevelopmentSeed__ManagementApiResource).");
        }

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["resource"] = _options.ManagementApiResource,
                ["scope"] = "all",
            });

        var endpoint = $"{_options.LogtoEndpoint.TrimEnd('/')}/oidc/token";
        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to get access token: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(body);

        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException(
                $"Failed to get access token: response did not contain access_token. {body}");
        }

        return tokenResponse.AccessToken;
    }

    public async Task<IReadOnlyList<LogtoUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_options.LogtoEndpoint.TrimEnd('/')}/api/users");

        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to get users: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        var users = JsonSerializer.Deserialize<IReadOnlyList<LogtoUser>>(body);

        if (users is null)
        {
            throw new InvalidOperationException(
                $"Failed to get users: response did not contain a valid users list. {body}");
        }

        return users;
    }

    public async Task<SeededUser> CreateUserAsync(CreateLogtoUser user, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.LogtoEndpoint.TrimEnd('/')}/api/users");

        request.Headers.Authorization = new("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(user), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
        var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine(body);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to create user: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException(
                $"Failed to create user: response did not contain a valid user. {body}");
        }

        var createdUser = JsonSerializer.Deserialize<SeededUser>(body);
        if (createdUser is null)
        {
            throw new InvalidOperationException(
                $"Failed to create user: response did not contain a valid user. {body}");
        }

        return createdUser;
    }

    public async Task<SeededUser> UpdateUserAsync(string userId, CreateLogtoUser user, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{_options.LogtoEndpoint.TrimEnd('/')}/api/users/{userId}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(user), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
        var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to update user: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException(
                $"Failed to update user: response did not contain a valid user. {body}");
        }

        var updatedUser = JsonSerializer.Deserialize<SeededUser>(body);
        if (updatedUser is null)
        {
            throw new InvalidOperationException(
                $"Failed to update user: response did not contain a valid user. {body}");
        }

        return updatedUser;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken);
}
