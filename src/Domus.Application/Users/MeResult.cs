using Domus.Application.Houses;
using Domus.Application.Tasks;

namespace Domus.Application.Users;

public sealed record MeResult(
    Guid Id,
    string FullName,
    bool NotifyDailyTasks,
    bool NotifyExpenses,
    bool NotifyFamilyChat,
    string Theme,
    IReadOnlyList<HouseMembershipSummary> Houses,
    IReadOnlyList<HouseTaskSummary> Tasks);
