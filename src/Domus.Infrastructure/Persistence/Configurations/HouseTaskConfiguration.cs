using Domus.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class HouseTaskConfiguration : IEntityTypeConfiguration<HouseTask>
{
    public void Configure(EntityTypeBuilder<HouseTask> task)
    {
        task.ToTable("house_tasks");

        task.HasKey(t => t.Id);

        task.Property(t => t.Id)
            .HasColumnName("id");

        task.Property(t => t.HouseId)
            .HasColumnName("house_id")
            .IsRequired();

        task.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        task.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        task.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        task.Property(t => t.DueAt)
            .HasColumnName("due_at");

        task.Property(t => t.CompletedAt)
            .HasColumnName("completed_at");

        task.Property(t => t.AssigneeUserId)
            .HasColumnName("assignee_user_id");

        task.Property(t => t.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        task.Property(t => t.CreatedAt)
            .HasColumnName("created_at");

        task.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        task.HasIndex(t => new { t.HouseId, t.Status });
        task.HasIndex(t => new { t.HouseId, t.DueAt });
        task.HasIndex(t => new { t.AssigneeUserId, t.Status });

        task.HasOne(t => t.House)
            .WithMany()
            .HasForeignKey(t => t.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        task.HasOne(t => t.CreatedByMembership)
            .WithMany()
            .HasForeignKey(t => new { t.CreatedByUserId, t.HouseId })
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        task.HasOne(t => t.AssigneeMembership)
            .WithMany()
            .HasForeignKey(t => new { t.AssigneeUserId, t.HouseId })
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}
