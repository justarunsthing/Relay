namespace Relay.Shared.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, UserResponse User);

public sealed record UserResponse(string Id, string Email, string DisplayName);

public sealed record AuthError(string Error);
