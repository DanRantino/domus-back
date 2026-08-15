namespace Domus.Infrastructure.DevelopmentSeed;

public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "DevelopmentSeed";

    public required string LogtoEndpoint { get; init; }

    public required string ManagementApiResource { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }
}
