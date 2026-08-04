namespace Domus.Domain.Users;

public static class UserThemes
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string System = "system";

    public static bool IsValid(string? theme) =>
        theme is Light or Dark or System;
}
