using System.Security.Claims;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Auth;

public interface IOrgAccessService
{
    Guid ResolveOrgId(ClaimsPrincipal user, Guid? requestedOrgId);
}

public class OrgAccessService : IOrgAccessService
{
    public Guid ResolveOrgId(ClaimsPrincipal user, Guid? requestedOrgId)
    {
        var ownOrgIdClaim = user.FindFirstValue(AppClaimTypes.OrgId);
        if (ownOrgIdClaim is null || !Guid.TryParse(ownOrgIdClaim, out var ownOrgId))
        {
            throw new InvalidOperationException("User is missing an org_id claim.");
        }

        var role = user.FindFirstValue(AppClaimTypes.AtsRole);
        if (role == nameof(ProfileRole.Admin) && requestedOrgId is not null)
        {
            return requestedOrgId.Value;
        }

        return ownOrgId;
    }
}
