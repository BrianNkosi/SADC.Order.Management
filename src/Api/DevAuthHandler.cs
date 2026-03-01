using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SADC.Order.Management.Api;

/// <summary>
/// Development-only authentication handler that auto-authenticates every request
/// with a fake admin identity. This allows testing APIs without Azure AD.
/// </summary>
public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "dev-user@sadc.local"),
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
            new Claim("roles", "Orders.Admin"),
            new Claim("roles", "Orders.Write"),
            new Claim("roles", "Orders.Read"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
