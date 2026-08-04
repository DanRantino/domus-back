using Domus.Application.Users;

namespace Domus.Api.Contracts.Users;

public sealed record MeResponse(
    Guid Id,
    string FullName,
    bool NotifyDailyTasks,
    bool NotifyExpenses,
    bool NotifyFamilyChat,
    string Theme,
    IReadOnlyList<MeHouseResponse> Houses)
{
    public static MeResponse FromApplication(MeResult result) =>
        new(
            result.Id,
            result.FullName,
            result.NotifyDailyTasks,
            result.NotifyExpenses,
            result.NotifyFamilyChat,
            result.Theme,
            result.Houses
                .Select(MeHouseResponse.FromApplication)
                .ToArray());
}