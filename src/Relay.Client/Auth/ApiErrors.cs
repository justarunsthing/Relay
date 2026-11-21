using System.Net.Http.Json;
using System.Text.Json;

namespace Relay.Client.Auth;

public static class ApiErrors
{
    public static async Task<string> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();

            if (!string.IsNullOrWhiteSpace(body?.Error))
            {
                return body.Error;
            }
        }
        catch (JsonException)
        {
        }

        return "The request failed.";
    }

    private sealed record ErrorBody(string? Error);
}
