using Domus.Api.Http;
using Domus.Application.Users;
using HotChocolate;
using ErrorCodes = Domus.Application.Common.ErrorCodes;

namespace Domus.Api.GraphQL;

public sealed class Query
{
    public async Task<Me> GetMe(
        IHttpContextAccessor httpContextAccessor,
        [Service] MeService meService,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new GraphQLException("HTTP context is required.");

        if (!CurrentUserContext.TryGet(httpContext, out var currentUser))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("User is not provisioned")
                    .SetCode(ErrorCodes.NotProvisioned)
                    .Build());
        }

        var result = await meService.GetAsync(
            currentUser.Id,
            currentUser.FullName,
            currentUser.NotifyDailyTasks,
            currentUser.NotifyExpenses,
            currentUser.NotifyFamilyChat,
            currentUser.Theme,
            cancellationToken);

        return Me.FromApplication(result.Value!);
    }
}
