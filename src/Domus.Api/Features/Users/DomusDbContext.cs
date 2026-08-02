using Microsoft.EntityFrameworkCore;

namespace Domus.Api.Features.Users;

public sealed class DomusDbContext(DbContextOptions<DomusDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("users");
        user.HasKey(u => u.Id);
        user.Property(u => u.Id).HasColumnName("id");
        user.Property(u => u.IdentityId)
            .HasColumnName("identity_id")
            .HasMaxLength(256)
            .IsRequired();
        user.HasIndex(u => u.IdentityId).IsUnique();
    }
}
