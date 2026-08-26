using Domus.Domain.Houses;
using Domus.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class HouseMembershipConfiguration
    : IEntityTypeConfiguration<HouseMembership>
{
    public void Configure(EntityTypeBuilder<HouseMembership> membership)
    {
        membership.ToTable("house_memberships");
        membership.HasKey(m => new { m.UserId, m.HouseId });
        membership.Property(m => m.UserId).HasColumnName("user_id");
        membership.Property(m => m.HouseId).HasColumnName("house_id");
        membership.Property(m => m.Role).HasColumnName("role").HasMaxLength(32).IsRequired();
        membership.Property(m => m.JoinedAt).HasColumnName("joined_at").IsRequired();
        membership.HasIndex(m => new { m.UserId, m.HouseId }).IsUnique();
        membership.HasOne(m => m.House).WithMany().HasForeignKey(m => m.HouseId).OnDelete(DeleteBehavior.Cascade);
        membership.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}