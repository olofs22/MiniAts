using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniAts.Api.Auth;
using MiniAts.Api.Dtos;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public ActionResult<MeResponse> Get()
    {
        var orgIdClaim = User.FindFirst(AppClaimTypes.OrgId)?.Value;
        var roleClaim = User.FindFirst(AppClaimTypes.AtsRole)?.Value;
        var sub = User.FindFirst("sub")?.Value;

        if (orgIdClaim is null || roleClaim is null || sub is null)
        {
            return Forbid();
        }

        return Ok(new MeResponse(Guid.Parse(sub), roleClaim, Guid.Parse(orgIdClaim)));
    }
}
