using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;
using MiniAts.Api.Supabase;
using Npgsql;

namespace MiniAts.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController(
    MiniAtsDbContext db,
    ISupabaseAdminAuthClient supabaseAdmin,
    IConfiguration config,
    ILogger<AdminUsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll([FromQuery] Guid? orgId)
    {
        var query = db.Profiles.AsNoTracking();
        if (orgId is not null)
        {
            query = query.Where(p => p.OrgId == orgId);
        }

        var profiles = await query.OrderBy(p => p.CreatedAt).ToListAsync();

        // Email lives only in Supabase Auth. One lookup per user is fine at MVP org sizes.
        var users = new List<UserResponse>(profiles.Count);
        foreach (var profile in profiles)
        {
            var supabaseUser = await supabaseAdmin.GetUserByIdAsync(profile.UserId);
            users.Add(new UserResponse(
                profile.UserId,
                supabaseUser?.Email ?? "(unknown)",
                profile.OrgId,
                profile.Role.ToString(),
                profile.CreatedAt));
        }

        return Ok(users);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            return NotFound();
        }

        // Ban first: if Supabase fails, the profile stays and the admin can retry,
        // rather than leaving a still-active login with no profile.
        await supabaseAdmin.BanUserAsync(userId);

        db.Profiles.Remove(profile);
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        if (!Enum.TryParse<ProfileRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest($"Invalid role '{request.Role}'.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required.");
        }

        if (request.OrgId is null && role == ProfileRole.Customer)
        {
            return BadRequest("OrgId is required for Customer users.");
        }

        // Organizations are always customer orgs; admins are platform-wide.
        if (request.OrgId is not null && role == ProfileRole.Admin)
        {
            return BadRequest("Admin users cannot belong to an organization.");
        }

        if (request.OrgId is not null)
        {
            var orgExists = await db.Organizations.AnyAsync(o => o.Id == request.OrgId);
            if (!orgExists)
            {
                return NotFound($"Organization '{request.OrgId}' not found.");
            }
        }

        var frontendUrl = config["App:FrontendUrl"];
        var redirectTo = frontendUrl is null ? null : $"{frontendUrl.TrimEnd('/')}/accept-invite";

        SupabaseUserResult supabaseUser;
        try
        {
            supabaseUser = await supabaseAdmin.InviteUserByEmailAsync(request.Email, redirectTo);
        }
        catch (SupabaseUserAlreadyExistsException)
        {
            var existing = await supabaseAdmin.FindUserByEmailAsync(request.Email);
            if (existing is null)
            {
                return Conflict($"'{request.Email}' is already registered in Supabase Auth, but the matching user could not be looked up.");
            }

            var existingProfile = await db.Profiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == existing.UserId);

            if (existingProfile is not null)
            {
                return Conflict(existingProfile.OrgId == request.OrgId
                    ? $"'{request.Email}' is already onboarded to this organization."
                    : $"'{request.Email}' is already onboarded to a different organization.");
            }

            // Supabase user exists but no Profile row - resume the previously
            // interrupted flow instead of failing again.
            return await CreateProfile(existing, request.OrgId, role);
        }
        catch (SupabaseAdminApiException ex)
        {
            logger.LogWarning(ex, "Supabase Admin Auth API rejected the invite for {Email}.", request.Email);
            return UnprocessableEntity($"Supabase rejected the invite for '{request.Email}': {ex.Message}");
        }

        return await CreateProfile(supabaseUser, request.OrgId, role);
    }

    private async Task<ActionResult<UserResponse>> CreateProfile(SupabaseUserResult supabaseUser, Guid? orgId, ProfileRole role)
    {
        var profile = new Profile { UserId = supabaseUser.UserId, OrgId = orgId, Role = role };
        db.Profiles.Add(profile);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Race: a concurrent request already inserted this Profile.
            var raced = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == supabaseUser.UserId);
            return raced is not null
                ? Conflict($"'{supabaseUser.Email}' is already onboarded.")
                : Problem("Profile insert failed with a unique violation, but no matching row was found.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex,
                "Supabase user {SupabaseUserId} ({Email}) was created but the Profile row could not be saved.",
                supabaseUser.UserId, supabaseUser.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, new CreateUserPartialFailureResponse(
                supabaseUser.UserId,
                supabaseUser.Email,
                orgId,
                role.ToString(),
                "The Supabase Auth user was created but the local Profile row could not be saved. " +
                "Retry this request (it will resume and only insert the Profile), or insert it manually."));
        }

        var response = new UserResponse(profile.UserId, supabaseUser.Email, profile.OrgId, profile.Role.ToString(), profile.CreatedAt);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
