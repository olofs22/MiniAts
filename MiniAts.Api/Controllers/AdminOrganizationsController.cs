using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;
using MiniAts.Api.Supabase;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/admin/organizations")]
[Authorize(Policy = "AdminOnly")]
public class AdminOrganizationsController(
    MiniAtsDbContext db,
    ISupabaseAdminAuthClient supabaseAdmin,
    ILogger<AdminOrganizationsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationResponse>>> GetAll()
    {
        var organizations = await db.Organizations.AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => ToResponse(o))
            .ToListAsync();

        return Ok(organizations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> GetById(Guid id)
    {
        var organization = await db.Organizations.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => ToResponse(o))
            .FirstOrDefaultAsync();

        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> Create(CreateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var organization = new Organization { Name = request.Name };

        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = organization.Id }, ToResponse(organization));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> Update(Guid id, UpdateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (organization is null)
        {
            return NotFound();
        }

        organization.Name = request.Name;
        await db.SaveChangesAsync();

        return Ok(ToResponse(organization));
    }

    // Deliberate cascade: the FKs to organizations are Restrict, so the DB still blocks
    // accidental org deletes anywhere else; only this endpoint removes dependents.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (organization is null)
        {
            return NotFound();
        }

        var profiles = await db.Profiles.Where(p => p.OrgId == id).ToListAsync();

        db.Applications.RemoveRange(await db.Applications.Where(a => a.OrgId == id).ToListAsync());
        db.Candidates.RemoveRange(await db.Candidates.Where(c => c.OrgId == id).ToListAsync());
        db.Jobs.RemoveRange(await db.Jobs.Where(j => j.OrgId == id).ToListAsync());
        db.Profiles.RemoveRange(profiles);
        db.Organizations.Remove(organization);

        await db.SaveChangesAsync();

        // Best-effort: the org's data is already gone; a failed ban just leaves a login
        // with no profile, which the API already rejects with 403.
        foreach (var profile in profiles)
        {
            try
            {
                await supabaseAdmin.BanUserAsync(profile.UserId);
            }
            catch (SupabaseAdminApiException ex)
            {
                logger.LogWarning(ex,
                    "Could not ban Supabase user {UserId} after deleting organization {OrgId}.",
                    profile.UserId, id);
            }
        }

        return NoContent();
    }

    private static OrganizationResponse ToResponse(Organization o) => new(o.Id, o.Name, o.CreatedAt);
}
