namespace Domus.Domain.Houses;

public static class HouseRoles
{
    public const string Admin = "admin";
    public const string Member = "member";
    public const string Guest = "guest";

    public static bool IsValid(string? role) =>
        role is Admin or Member or Guest;
}
