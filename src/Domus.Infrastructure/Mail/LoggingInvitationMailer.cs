using Domus.Application.Houses;
using Microsoft.Extensions.Logging;

namespace Domus.Infrastructure.Mail;

public sealed class LoggingInvitationMailer(
    ILogger<LoggingInvitationMailer> logger) : IInvitationMailer
{
    public Task<bool> SendAsync(InvitationEmail email, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Invitation email skipped (no Resend API key). House={House}",
            email.HouseName);
        return Task.FromResult(true);
    }
}
