using System.Security.Claims;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseResourceSchedulingSystem.Web.Security;

public sealed class AccountCookieEvents(IAccountRepository? accounts = null) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return;
        }

        if (accounts is null ||
            context.Principal?.Identity is not ClaimsIdentity identity ||
            !Guid.TryParse(identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            await RejectAsync(context);
            return;
        }

        var currentLevel = await accounts.FindActiveLevelAsync(userId, context.HttpContext.RequestAborted);
        if (currentLevel is null)
        {
            await RejectAsync(context);
            return;
        }

        if (!context.Principal.IsInRole(currentLevel))
        {
            var updatedIdentity = new ClaimsIdentity(identity);
            foreach (var role in updatedIdentity.FindAll(updatedIdentity.RoleClaimType).ToArray())
            {
                updatedIdentity.RemoveClaim(role);
            }

            updatedIdentity.AddClaim(new Claim(updatedIdentity.RoleClaimType, currentLevel));
            context.ReplacePrincipal(new ClaimsPrincipal(updatedIdentity));
            context.ShouldRenew = true;
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
