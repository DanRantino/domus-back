using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Domus.Api.Features.Users;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this WebApplication app)
    {
        app.MapGet("/me", GetMeAsync).RequireAuthorization();
        app.MapPost("/me", ProvisionMeAsync).RequireAuthorization();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        DomusDbContext db,
        CancellationToken cancellationToken)
    {
        var identityId = GetSubject(principal);
        if (identityId is null)
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);

        if (user is null)
        {
            // Authenticated at IdP but not provisioned in Domus — not an auth failure.
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return Results.Ok(ToResponse(user));
    }

    private static async Task<IResult> ProvisionMeAsync(
        ClaimsPrincipal principal,
        DomusDbContext db,
        CancellationToken cancellationToken)
    {
        // identity_id is derived only from the authenticated token subject.
        // Any client body (including a forged identity_id) is ignored.
        var identityId = GetSubject(principal);
        if (identityId is null)
        {
            return Results.Unauthorized();
        }

        var existing = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);

        if (existing is not null)
        {
            return Results.Conflict();
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var raced = await db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);

            if (raced is not null)
            {
                return Results.Conflict();
            }

            throw;
        }

        return Results.Created("/me", ToResponse(user));
    }

    private static string? GetSubject(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.IdentityId);
}
