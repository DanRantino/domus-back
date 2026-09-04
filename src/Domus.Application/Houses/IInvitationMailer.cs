namespace Domus.Application.Houses;

public interface IInvitationMailer
{
    Task<bool> SendAsync(InvitationEmail email, CancellationToken cancellationToken);
}
