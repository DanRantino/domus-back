using Domus.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domus.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> expense)
    {
        expense.ToTable("expenses");
        expense.HasKey(x => x.Id);
        expense.Property(x => x.Id).HasColumnName("id");
        expense.Property(x => x.HouseId).HasColumnName("house_id");
        expense.Property(x => x.Description).HasColumnName("description").HasMaxLength(512).IsRequired();
        expense.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        expense.Property(x => x.Category).HasColumnName("category").HasMaxLength(128).IsRequired();
        expense.Property(x => x.Date).HasColumnName("date").IsRequired();
        expense.Property(x => x.PaidByUserId).HasColumnName("paid_by_user_id");
        expense.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        expense.Property(x => x.CreatedAt).HasColumnName("created_at");
        expense.HasIndex(x => new { x.HouseId, x.Date });
    }
}
