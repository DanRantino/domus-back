using Domus.Application.Users;

namespace Domus.Api.Http;

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserStore userStore)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.TryGetIdentityId(out var identityId))
        {
            var user = await userStore.FindByIdentityIdAsync(
                identityId,
                context.RequestAborted);

            if (user is not null)
            {
                context.Items[CurrentUserContext.ItemKey] = new CurrentUser(
                    user.Id,
                    user.IdentityId,
                    user.FullName,
                    user.NotifyDailyTasks,
                    user.NotifyExpenses,
                    user.NotifyFamilyChat,
                    user.Theme);
            }
        }

        await next(context);
    }
}
