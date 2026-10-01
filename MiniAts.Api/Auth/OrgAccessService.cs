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
        var role = user.FindFirstValue(AppClaimTypes.AtsRole);
        var ownOrgIdClaim = user.FindFirstValue(AppClaimTypes.OrgId);
        var hasOwnOrgId = Guid.TryParse(ownOrgIdClaim, out var ownOrgId);

        if (role == nameof(ProfileRole.Admin))
        {
            if (requestedOrgId is not null)
            {
                return requestedOrgId.Value;
            }

            if (hasOwnOrgId)
            {
                return ownOrgId;
            }

            throw new InvalidOperationException(
                "Admin has no home organization; an orgId must be specified.");
        }

        if (!hasOwnOrgId)
        {
            throw new InvalidOperationException("User is missing an org_id claim.");
        }

        return ownOrgId;
    }
}
