using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Data;

namespace MiniAts.Api.Auth;

public class ProfileClaimsTransformation(MiniAtsDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        if (principal.HasClaim(c => c.Type == AppClaimTypes.OrgId))
        {
            return principal;
        }

        var sub = principal.FindFirstValue("sub");
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return principal;
        }

        var profile = await db.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            return principal;
        }

        var identity = (ClaimsIdentity)principal.Identity;
        identity.AddClaim(new Claim(AppClaimTypes.OrgId, profile.OrgId.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.AtsRole, profile.Role.ToString()));

        return principal;
    }
}
