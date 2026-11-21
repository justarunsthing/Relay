using System.Net.Http.Headers;

namespace Relay.Client.Auth;

public sealed class BearerTokenHandler(RelayAuthenticationStateProvider auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await auth.EnsureLoadedAsync();

        if (!string.IsNullOrEmpty(auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        var path = request.RequestUri?.AbsolutePath;
        var isCredentialPost = path is "/api/auth/login" or "/api/auth/register";

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && request.Headers.Authorization is not null
            && !isCredentialPost)
        {
            await auth.SignOutAsync();
        }

        return response;
    }
}
