namespace Domus.Api.Http;

public sealed record CurrentUser(
    Guid Id,
    string IdentityId,
    string? FullName,
    bool NotifyDailyTasks,
    bool NotifyExpenses,
    bool NotifyFamilyChat,
    string Theme);
