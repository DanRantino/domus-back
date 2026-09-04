using Domus.Domain.Houses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class HouseInvitationConfiguration
    : IEntityTypeConfiguration<HouseInvitation>
{
    public void Configure(EntityTypeBuilder<HouseInvitation> invitation)
    {
        invitation.ToTable("house_invitations");

        invitation.HasKey(i => i.Id);

        invitation.Property(i => i.Id)
            .HasColumnName("id");

        invitation.Property(i => i.HouseId)
            .HasColumnName("house_id");

        invitation.Property(i => i.InvitedByUserId)
            .HasColumnName("invited_by_user_id");

        invitation.Property(i => i.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        invitation.Property(i => i.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .IsRequired();

        invitation.Property(i => i.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        invitation.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        invitation.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at");

        invitation.Property(i => i.CreatedAt)
            .HasColumnName("created_at");

        invitation.Property(i => i.AcceptedAt)
            .HasColumnName("accepted_at");

        invitation.Property(i => i.AcceptedByUserId)
            .HasColumnName("accepted_by_user_id");

        invitation.HasIndex(i => i.TokenHash)
            .IsUnique();

        invitation.HasIndex(i => new { i.HouseId, i.Email })
            .IsUnique()
            .HasFilter("status = 'pending'")
            .HasDatabaseName("ix_house_invitations_house_id_email_pending");

        invitation.HasOne(i => i.House)
            .WithMany()
            .HasForeignKey(i => i.HouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
