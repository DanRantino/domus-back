using Domus.Api.Http;

namespace Domus.Api.Tests;

public sealed class LocalReturnUrlTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("/dashboard?next=1", "/dashboard?next=1")]
    public void TryResolve_AcceptsLocalPaths(string? returnUrl, string expected)
    {
        Assert.True(LocalReturnUrl.TryResolve(returnUrl, out var resolved));
        Assert.Equal(expected, resolved);
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil")]
    [InlineData("dashboard")]
    public void TryResolve_RejectsExternalOrRelativeUrls(string returnUrl)
    {
        Assert.False(LocalReturnUrl.TryResolve(returnUrl, out _));
    }
}
