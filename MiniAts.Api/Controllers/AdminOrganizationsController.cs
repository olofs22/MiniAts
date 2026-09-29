using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/admin/organizations")]
[Authorize(Policy = "AdminOnly")]
public class AdminOrganizationsController(MiniAtsDbContext db) : ControllerBase
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

    private static OrganizationResponse ToResponse(Organization o) => new(o.Id, o.Name, o.CreatedAt);
}
