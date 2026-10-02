using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/admin/signup-requests")]
[Authorize(Policy = "AdminOnly")]
public class AdminSignupRequestsController(MiniAtsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SignupRequestResponse>>> GetAll()
    {
        var requests = await db.SignupRequests.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new SignupRequestResponse(r.Id, r.CompanyName, r.ContactName, r.Email, r.Message, r.CreatedAt))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = await db.SignupRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        db.SignupRequests.Remove(request);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
