using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController(MiniAtsDbContext db, IOrgAccessService orgAccess) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetAll([FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var jobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrgId == effectiveOrgId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => ToResponse(j))
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetById(Guid id, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var job = await db.Jobs.AsNoTracking()
            .Where(j => j.Id == id && j.OrgId == effectiveOrgId)
            .Select(j => ToResponse(j))
            .FirstOrDefaultAsync();

        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<JobResponse>> Create(CreateJobRequest request, [FromQuery] Guid? orgId)
    {
        if (!TryParseStatus(request.Status, JobStatus.Open, out var status))
        {
            return BadRequest($"Invalid status '{request.Status}'.");
        }

        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var job = new Job
        {
            OrgId = effectiveOrgId,
            Title = request.Title,
            Description = request.Description,
            Status = status
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        var response = new JobResponse(job.Id, job.OrgId, job.Title, job.Description, job.Status.ToString(), job.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateJobRequest request, [FromQuery] Guid? orgId)
    {
        if (!TryParseStatus(request.Status, null, out var status))
        {
            return BadRequest($"Invalid status '{request.Status}'.");
        }

        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.OrgId == effectiveOrgId);
        if (job is null)
        {
            return NotFound();
        }

        job.Title = request.Title;
        job.Description = request.Description;
        job.Status = status;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? orgId)
    {
        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.OrgId == effectiveOrgId);
        if (job is null)
        {
            return NotFound();
        }

        db.Jobs.Remove(job);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static JobResponse ToResponse(Job j) =>
        new(j.Id, j.OrgId, j.Title, j.Description, j.Status.ToString(), j.CreatedAt);

    private static bool TryParseStatus(string? value, JobStatus? fallback, out JobStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (fallback is null)
            {
                status = default;
                return false;
            }

            status = fallback.Value;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out status);
    }
}
