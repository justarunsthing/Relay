using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Relay.Api.Data;
using Relay.Shared.Auth;

namespace Relay.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(RegisterRequest request, UserManager<ApplicationUser> users)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new AuthError("Email, password, and display name are required."));
        }

        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await users.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Results.BadRequest(new AuthError(string.Join(' ', result.Errors.Select(error => error.Description))));
        }

        return Results.Created("/api/auth/me", ToResponse(user));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> users,
        JwtTokenService tokens)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new AuthError("Email and password are required."));
        }

        var user = await users.FindByEmailAsync(request.Email.Trim());

        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        {
            return Results.Json(
                new AuthError("Invalid email or password."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(new LoginResponse(tokens.CreateToken(user), ToResponse(user)));
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, UserManager<ApplicationUser> users)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var user = await users.FindByIdAsync(userId);

        return user is null ? Results.Unauthorized() : Results.Ok(ToResponse(user));
    }

    private static UserResponse ToResponse(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName);
}
