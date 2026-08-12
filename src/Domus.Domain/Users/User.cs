using System.Diagnostics.CodeAnalysis;

namespace Domus.Domain.Users;

public sealed class User
{
    public Guid Id { get; set; }

    public required string IdentityId { get; set; }

    public string? FullName { get; set; }

    public string Theme { get; set; } = UserThemes.System;

    public bool NotifyDailyTasks { get; set; } = true;

    public bool NotifyExpenses { get; set; } = true;

    public bool NotifyFamilyChat { get; set; } = true;

    private User() {}

    [SetsRequiredMembers]
    public User(Guid id, string identityId, string? fullName)
    {
        Id = id;
        IdentityId = identityId;
        FullName = fullName;

        NotifyDailyTasks = true;
        NotifyExpenses = true;
        NotifyFamilyChat = true;
        Theme = UserThemes.System;
    }
}
