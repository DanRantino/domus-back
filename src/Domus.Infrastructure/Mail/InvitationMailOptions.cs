namespace Domus.Infrastructure.Mail;

public sealed class InvitationMailOptions
{
    public const string SectionName = "Invitations";

    public string FrontendOrigin { get; set; } = "https://web.domus.dev";
}
