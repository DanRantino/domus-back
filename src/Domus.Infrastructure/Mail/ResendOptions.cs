namespace Domus.Infrastructure.Mail;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;
}
