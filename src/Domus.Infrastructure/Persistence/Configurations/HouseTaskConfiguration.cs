using Domus.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class HouseTaskConfiguration : IEntityTypeConfiguration<HouseTask>
{
    public void Configure(EntityTypeBuilder<HouseTask> task)
    {
        task.ToTable("house_tasks");
        task.HasKey(x => x.Id);
        task.Property(x => x.Id).HasColumnName("id");
        task.Property(x => x.HouseId).HasColumnName("house_id");
        task.Property(x => x.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
        task.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        task.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        task.Property(x => x.DueAt).HasColumnName("due_at");
        task.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id");
        task.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        task.Property(x => x.CreatedAt).HasColumnName("created_at");
        task.Property(x => x.CompletedAt).HasColumnName("completed_at");
        task.HasIndex(x => new { x.HouseId, x.Status });
    }
}
