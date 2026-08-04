using Domus.Application.Houses;

namespace Domus.Application.Users;

public sealed record MeResult(
    Guid Id,
    string FullName,
    bool NotifyDailyTasks,
    bool NotifyExpenses,
    bool NotifyFamilyChat,
    string Theme,
    IReadOnlyList<HouseMembershipSummary> Houses);
