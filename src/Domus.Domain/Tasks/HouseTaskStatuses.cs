namespace Domus.Domain.Tasks;

public static class HouseTaskStatuses
{
    public const string Pending = "pending";
    public const string Completed = "completed";

    public static bool IsValid(string? status) =>
        status is Pending or Completed;
}
