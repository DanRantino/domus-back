using Domus.Domain.Houses;
using Domus.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Domus.Infrastructure.Persistence;

public sealed class DomusDbContext(
    DbContextOptions<DomusDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<House> Houses => Set<House>();

    public DbSet<HouseMembership> HouseMemberships => Set<HouseMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DomusDbContext).Assembly);
    }
}