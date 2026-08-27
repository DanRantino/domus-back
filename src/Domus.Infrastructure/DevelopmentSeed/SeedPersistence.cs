using Domus.Infrastructure.Persistence;

namespace Domus.Infrastructure.DevelopmentSeed;

internal static class SeedPersistence
{
    public static async Task SaveIfChangedAsync(
        this DomusDbContext db,
        CancellationToken cancellationToken)
    {
        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
