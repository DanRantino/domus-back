namespace Domus.Domain.Expenses;

public sealed class Expense
{
    public Guid Id { get; set; }
    public Guid HouseId { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public required string Category { get; set; }
    public DateOnly Date { get; set; }
    public Guid PaidByUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
