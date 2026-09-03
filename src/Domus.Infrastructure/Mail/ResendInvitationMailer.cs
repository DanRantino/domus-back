using System.Net.Http.Json;
using System.Text.Json;
using Domus.Application.Houses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Domus.Infrastructure.Mail;

public sealed class ResendInvitationMailer(
    HttpClient httpClient,
    IOptions<ResendOptions> resendOptions,
    IOptions<InvitationMailOptions> invitationOptions,
    ILogger<ResendInvitationMailer> logger) : IInvitationMailer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<bool> SendAsync(InvitationEmail email, CancellationToken cancellationToken)
    {
        var options = resendOptions.Value;
        var origin = invitationOptions.Value.FrontendOrigin.TrimEnd('/');
        var acceptUrl = $"{origin}/start/invite?token={Uri.EscapeDataString(email.Token)}";
        var inviter = string.IsNullOrWhiteSpace(email.InviterName)
            ? "Um administrador"
            : email.InviterName.Trim();

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(
            new ResendEmailRequest(
                options.From,
                [email.To],
                $"Convite para {email.HouseName}",
                BuildHtml(email.HouseName, inviter, acceptUrl, email.Token)),
            options: JsonOptions);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Resend rejected invitation email to {To}: {Status} {Body}",
                email.To,
                (int)response.StatusCode,
                body);
            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Failed to send invitation email to {To}", email.To);
            return false;
        }
    }

    private static string BuildHtml(
        string houseName,
        string inviter,
        string acceptUrl,
        string token) =>
        $"""
        <p>{System.Net.WebUtility.HtmlEncode(inviter)} convidou você para a Domus {System.Net.WebUtility.HtmlEncode(houseName)}.</p>
        <p><a href="{System.Net.WebUtility.HtmlEncode(acceptUrl)}">Entrar na casa</a></p>
        <p>Ou cole este código em /start/invite: <strong>{System.Net.WebUtility.HtmlEncode(token)}</strong></p>
        <p>Use o mesmo e-mail deste convite ao entrar.</p>
        """;

    private sealed record ResendEmailRequest(
        string From,
        IReadOnlyList<string> To,
        string Subject,
        string Html);
}
