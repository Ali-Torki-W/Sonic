using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sonic.Application.Common.Errors;

namespace Sonic.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            throw Errors.Unauthorized("User id claim is missing.", "auth.missing_sub");
        }

        return sub;
    }

    protected bool IsCurrentUserAdmin()
    {
        var role = User.FindFirst("role")?.Value
                   ?? User.FindFirst(ClaimTypes.Role)?.Value;

        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
