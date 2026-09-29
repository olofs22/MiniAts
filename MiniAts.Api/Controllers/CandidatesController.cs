using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/candidates")]
[Authorize]
public class CandidatesController(MiniAtsDbContext db, IOrgAccessService orgAccess) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateResponse>>> GetAll([FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var candidates = await db.Candidates.AsNoTracking()
            .Where(c => c.OrgId == effectiveOrgId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return Ok(candidates);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateResponse>> GetById(Guid id, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var candidate = await db.Candidates.AsNoTracking()
            .Where(c => c.Id == id && c.OrgId == effectiveOrgId)
            .Select(c => ToResponse(c))
            .FirstOrDefaultAsync();

        return candidate is null ? NotFound() : Ok(candidate);
    }

    [HttpPost]
    public async Task<ActionResult<CandidateResponse>> Create(CreateCandidateRequest request, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var candidate = new Candidate
        {
            OrgId = effectiveOrgId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            LinkedInUrl = request.LinkedInUrl,
            Notes = request.Notes
        };

        db.Candidates.Add(candidate);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, ToResponse(candidate));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCandidateRequest request, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == id && c.OrgId == effectiveOrgId);
        if (candidate is null)
        {
            return NotFound();
        }

        candidate.Name = request.Name;
        candidate.Email = request.Email;
        candidate.Phone = request.Phone;
        candidate.LinkedInUrl = request.LinkedInUrl;
        candidate.Notes = request.Notes;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == id && c.OrgId == effectiveOrgId);
        if (candidate is null)
        {
            return NotFound();
        }

        db.Candidates.Remove(candidate);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static CandidateResponse ToResponse(Candidate c) =>
        new(c.Id, c.OrgId, c.Name, c.Email, c.Phone, c.LinkedInUrl, c.Notes, c.CreatedAt);
}
