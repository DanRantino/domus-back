namespace Domus.Domain.Houses;

public sealed class House
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}
