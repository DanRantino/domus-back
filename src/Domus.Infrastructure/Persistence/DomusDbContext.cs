using Domus.Domain.Events;
using Domus.Domain.Expenses;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;
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
    public DbSet<HouseTask> HouseTasks => Set<HouseTask>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<HouseEvent> HouseEvents => Set<HouseEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DomusDbContext).Assembly);
    }
}