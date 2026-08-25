namespace Domus.Infrastructure.Identity;

public sealed record LogtoUser(
    string id,
    string? username,
    string? primaryEmail,
    long? primaryPhone,
    string? name,
    string? avatar,
    Dictionary<string, object> customData,
    Dictionary<string, object> identities,
    long? lastSignInAt,
    long createdAt,
    long updatedAt,
    Dictionary<string, object> profile,
    string? applicationId,
    bool isSuspended,
    bool hasPassword
);
