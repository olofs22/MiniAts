using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(MiniAtsDbContext db, IOrgAccessService orgAccess) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetAll(
        [FromQuery] Guid? orgId, [FromQuery] Guid? jobId, [FromQuery] Guid? candidateId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var query = db.Applications.AsNoTracking().Where(a => a.OrgId == effectiveOrgId);

        if (jobId is not null)
        {
            query = query.Where(a => a.JobId == jobId);
        }

        if (candidateId is not null)
        {
            query = query.Where(a => a.CandidateId == candidateId);
        }

        var applications = await query
            .OrderBy(a => a.Stage)
            .ThenBy(a => a.Position)
            .Select(a => ToResponse(a))
            .ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(Guid id, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var application = await db.Applications.AsNoTracking()
            .Where(a => a.Id == id && a.OrgId == effectiveOrgId)
            .Select(a => ToResponse(a))
            .FirstOrDefaultAsync();

        return application is null ? NotFound() : Ok(application);
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(CreateApplicationRequest request, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var candidateExists = await db.Candidates
            .AnyAsync(c => c.Id == request.CandidateId && c.OrgId == effectiveOrgId);
        if (!candidateExists)
        {
            return NotFound($"Candidate '{request.CandidateId}' not found.");
        }

        var jobExists = await db.Jobs
            .AnyAsync(j => j.Id == request.JobId && j.OrgId == effectiveOrgId);
        if (!jobExists)
        {
            return NotFound($"Job '{request.JobId}' not found.");
        }

        var maxPosition = await db.Applications
            .Where(a => a.JobId == request.JobId && a.Stage == ApplicationStage.New)
            .Select(a => (double?)a.Position)
            .MaxAsync();

        var application = new Application
        {
            OrgId = effectiveOrgId,
            CandidateId = request.CandidateId,
            JobId = request.JobId,
            Stage = ApplicationStage.New,
            Position = (maxPosition ?? -1) + 1
        };

        db.Applications.Add(application);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = application.Id }, ToResponse(application));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateApplicationRequest request, [FromQuery] Guid? orgId)
    {
        if (!Enum.TryParse<ApplicationStage>(request.Stage, ignoreCase: true, out var stage))
        {
            return BadRequest($"Invalid stage '{request.Stage}'.");
        }

        // Negative values are legitimate (dropping above the first card yields first - 1).
        if (!double.IsFinite(request.Position))
        {
            return BadRequest("Position must be a finite number.");
        }

        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.OrgId == effectiveOrgId);
        if (application is null)
        {
            return NotFound();
        }

        application.Stage = stage;
        application.Position = request.Position;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.OrgId == effectiveOrgId);
        if (application is null)
        {
            return NotFound();
        }

        db.Applications.Remove(application);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ApplicationResponse ToResponse(Application a) =>
        new(a.Id, a.OrgId, a.CandidateId, a.JobId, a.Stage.ToString(), a.Position, a.CreatedAt);
}
