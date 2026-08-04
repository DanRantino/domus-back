using Domus.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> user)
    {
        user.ToTable("users");

        user.HasKey(u => u.Id);

        user.Property(u => u.Id)
            .HasColumnName("id");

        user.Property(u => u.IdentityId)
            .HasColumnName("identity_id")
            .HasMaxLength(256)
            .IsRequired();

        user.HasIndex(u => u.IdentityId)
            .IsUnique();

        user.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(256);

        user.Property(u => u.Theme)
            .HasColumnName("theme")
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(UserThemes.System);

        user.Property(u => u.NotifyDailyTasks)
            .HasColumnName("notify_daily_tasks")
            .IsRequired()
            .HasDefaultValue(true);

        user.Property(u => u.NotifyExpenses)
            .HasColumnName("notify_expenses")
            .IsRequired()
            .HasDefaultValue(true);

        user.Property(u => u.NotifyFamilyChat)
            .HasColumnName("notify_family_chat")
            .IsRequired()
            .HasDefaultValue(true);
    }
}