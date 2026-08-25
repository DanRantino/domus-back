using System.Text.Json;
using Domus.Infrastructure.Identity;

namespace Domus.Api.Tests;

public sealed class LogtoUserDeserializationTests
{
    [Fact]
    public void GetUsers_DeserializesUnixTimestampLastSignInAt()
    {
        const string json = """
            [{
              "id": "user-1",
              "username": "alice",
              "primaryEmail": "alice@example.com",
              "primaryPhone": null,
              "name": "Alice",
              "avatar": null,
              "customData": {},
              "identities": {},
              "lastSignInAt": 1700000000000,
              "createdAt": 1700000000000,
              "updatedAt": 1700000000000,
              "profile": {},
              "applicationId": null,
              "isSuspended": false,
              "hasPassword": true
            }]
            """;

        var users = JsonSerializer.Deserialize<IReadOnlyList<LogtoUser>>(json);

        Assert.NotNull(users);
        var user = Assert.Single(users);
        Assert.Equal("user-1", user.id);
        Assert.Equal(1700000000000, user.lastSignInAt);
    }

    [Fact]
    public void GetUsers_DeserializesNullLastSignInAt()
    {
        const string json = """
            [{
              "id": "user-2",
              "username": "bob",
              "primaryEmail": "bob@example.com",
              "primaryPhone": null,
              "name": "Bob",
              "avatar": null,
              "customData": {},
              "identities": {},
              "lastSignInAt": null,
              "createdAt": 1700000000000,
              "updatedAt": 1700000000000,
              "profile": {},
              "applicationId": null,
              "isSuspended": false,
              "hasPassword": true
            }]
            """;

        var users = JsonSerializer.Deserialize<IReadOnlyList<LogtoUser>>(json);

        Assert.NotNull(users);
        var user = Assert.Single(users);
        Assert.Null(user.lastSignInAt);
    }
}
