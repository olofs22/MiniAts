using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(MiniAtsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get()
    {
        var orgIdClaim = User.FindFirst(AppClaimTypes.OrgId)?.Value;
        var roleClaim = User.FindFirst(AppClaimTypes.AtsRole)?.Value;
        var sub = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;

        if (roleClaim is null || sub is null || email is null)
        {
            return Forbid();
        }

        Guid? orgId = Guid.TryParse(orgIdClaim, out var parsedOrgId) ? parsedOrgId : null;
        string? orgName = null;

        if (orgId is not null)
        {
            orgName = await db.Organizations.AsNoTracking()
                .Where(o => o.Id == orgId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();

            if (orgName is null)
            {
                return Forbid();
            }
        }

        return Ok(new MeResponse(Guid.Parse(sub), email, roleClaim, orgId, orgName));
    }
}
