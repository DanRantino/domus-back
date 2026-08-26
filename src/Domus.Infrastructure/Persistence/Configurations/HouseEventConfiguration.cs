using Domus.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class HouseEventConfiguration : IEntityTypeConfiguration<HouseEvent>
{
    public void Configure(EntityTypeBuilder<HouseEvent> houseEvent)
    {
        houseEvent.ToTable("house_events");
        houseEvent.HasKey(x => x.Id);
        houseEvent.Property(x => x.Id).HasColumnName("id");
        houseEvent.Property(x => x.HouseId).HasColumnName("house_id");
        houseEvent.Property(x => x.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
        houseEvent.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        houseEvent.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
        houseEvent.Property(x => x.EndsAt).HasColumnName("ends_at");
        houseEvent.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        houseEvent.Property(x => x.CreatedAt).HasColumnName("created_at");
        houseEvent.HasIndex(x => new { x.HouseId, x.StartsAt });
    }
}
