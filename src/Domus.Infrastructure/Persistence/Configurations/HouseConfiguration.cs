using Domus.Domain.Houses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class HouseConfiguration : IEntityTypeConfiguration<House>
{
    public void Configure(EntityTypeBuilder<House> house)
    {
        house.ToTable("houses");

        house.HasKey(h => h.Id);

        house.Property(h => h.Id)
            .HasColumnName("id");

        house.Property(h => h.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();
    }
}