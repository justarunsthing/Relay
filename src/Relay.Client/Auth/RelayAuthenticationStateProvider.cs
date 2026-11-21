using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Relay.Client.Auth;

public sealed class RelayAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private Task? _ready;
    private string? _token;

    public RelayAuthenticationStateProvider(IJSRuntime js)
    {
        _js = js;
    }

    public string? Token => _token;

    public Task EnsureLoadedAsync() => _ready ??= LoadAsync();

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureLoadedAsync();

        return new AuthenticationState(CreatePrincipal(_token));
    }

    public async Task SignInAsync(string token)
    {
        await EnsureLoadedAsync();
        _token = token;
        await _js.InvokeVoidAsync("relayToken.set", token);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await EnsureLoadedAsync();
        _token = null;
        await _js.InvokeVoidAsync("relayToken.remove");

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task LoadAsync()
    {
        var stored = await _js.InvokeAsync<string?>("relayToken.get");

        if (ReadUsableToken(stored) is null)
        {
            _token = null;

            if (!string.IsNullOrWhiteSpace(stored))
            {
                await _js.InvokeVoidAsync("relayToken.remove");
            }

            return;
        }

        _token = stored;
    }

    private static string? ReadUsableToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return TryReadExpiry(token, out var expiresAt) && expiresAt > DateTimeOffset.UtcNow
            ? token
            : null;
    }

    private static ClaimsPrincipal CreatePrincipal(string? token)
    {
        if (token is null || !TryReadPayload(token, out var payload))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>();

        if (payload.TryGetProperty("sub", out var subject) && subject.GetString() is { Length: > 0 } subjectId)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, subjectId));
        }

        if (payload.TryGetProperty("email", out var email) && email.GetString() is { Length: > 0 } emailAddress)
        {
            claims.Add(new Claim(ClaimTypes.Email, emailAddress));
        }

        if (payload.TryGetProperty("display_name", out var displayName) && displayName.GetString() is { Length: > 0 } name)
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt"));
    }

    private static bool TryReadExpiry(string token, out DateTimeOffset expiresAt)
    {
        expiresAt = default;

        if (!TryReadPayload(token, out var payload) || !payload.TryGetProperty("exp", out var exp) || !exp.TryGetInt64(out var seconds))
        {
            return false;
        }

        expiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }

    private static bool TryReadPayload(string token, out JsonElement payload)
    {
        payload = default;

        var parts = token.Split('.');

        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(DecodeBase64Url(parts[1]));
            using var document = JsonDocument.Parse(json);

            payload = document.RootElement.Clone();

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;

        if (remainder > 0)
        {
            padded = padded.PadRight(padded.Length + (4 - remainder), '=');
        }

        return Convert.FromBase64String(padded);
    }
}
