using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Relay.Api.Auth;

public static class CurrentUser
{
    public static string? Id(ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
