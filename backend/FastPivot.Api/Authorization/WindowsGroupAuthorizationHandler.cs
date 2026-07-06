using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FastPivot.Api.Authorization;

public sealed class WindowsGroupAuthorizationHandler : AuthorizationHandler<WindowsGroupRequirement>
{
    private static readonly HashSet<string> EveryoneAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "*",
        "Everyone"
    };

    private readonly IOptionsMonitor<WindowsAuthorizationOptions> _options;

    public WindowsGroupAuthorizationHandler(IOptionsMonitor<WindowsAuthorizationOptions> options)
    {
        _options = options;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WindowsGroupRequirement requirement)
    {
        var allowedGroups = _options.CurrentValue.AllowedGroups
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group.Trim())
            .ToArray();

        if (allowedGroups.Length == 0 || allowedGroups.Any(EveryoneAliases.Contains))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (allowedGroups.Any(group => IsUserInGroup(context.User, group)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsUserInGroup(ClaimsPrincipal user, string group)
    {
        return user.IsInRole(group)
            || user.Claims.Any(claim => IsGroupClaim(claim) && string.Equals(claim.Value, group, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGroupClaim(Claim claim)
    {
        return claim.Type is ClaimTypes.GroupSid or ClaimTypes.Role
            || claim.Type.Contains("group", StringComparison.OrdinalIgnoreCase);
    }
}
