using Domus.Api.Contracts.Bff;
using Domus.Api.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("bff")]
[Produces("application/json")]
public sealed class BffController : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        if (!LocalReturnUrl.TryResolve(returnUrl, out var redirectUri))
        {
            return BadRequest();
        }

        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        return SignOut(
            properties,
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("session")]
    [Authorize]
    [ProducesResponseType(typeof(BffSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<BffSessionResponse> Session()
    {
        return Ok(new BffSessionResponse(
            Authenticated: true,
            Picture: User.FindFirst("picture")?.Value,
            Name: User.FindFirst("name")?.Value,
            Username: User.FindFirst("username")?.Value
                ?? User.FindFirst("preferred_username")?.Value));
    }
}
