using Microsoft.AspNetCore.Identity;

namespace Relay.Api.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
