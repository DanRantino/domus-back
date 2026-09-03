using Domus.Api.Contracts.Auth;
using Logto.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl) =>
        Challenge(
            new AuthenticationProperties
            {
                RedirectUri = SanitizeReturnUrl(returnUrl),
            },
            LogtoDefaults.AuthenticationScheme);

    [HttpGet("logout")]
    [AllowAnonymous]
    public IActionResult Logout([FromQuery] string? returnUrl) =>
        SignOut(
            new AuthenticationProperties
            {
                RedirectUri = SanitizeReturnUrl(returnUrl),
            });

    [HttpGet("session")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    public ActionResult<AuthSessionResponse> Session()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new AuthSessionResponse(false, null, null));
        }

        return Ok(
            new AuthSessionResponse(
                true,
                User.FindFirst("picture")?.Value,
                User.FindFirst("name")?.Value));
    }

    private static string SanitizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//")
            || returnUrl.StartsWith("/\\")
            || returnUrl.Contains('\\')
            || returnUrl.Contains("://", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}
