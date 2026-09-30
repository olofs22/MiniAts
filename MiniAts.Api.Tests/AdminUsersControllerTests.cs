using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MiniAts.Api.Controllers;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;
using MiniAts.Api.Supabase;

namespace MiniAts.Api.Tests;

public class AdminUsersControllerTests
{
    private static AdminUsersController CreateController(MiniAtsDbContext db, FakeSupabaseAdminAuthClient supabase) =>
        new(db, supabase, NullLogger<AdminUsersController>.Instance);

    private static async Task<Organization> SeedOrganization(MiniAtsDbContext db, string name = "Acme Inc")
    {
        var org = new Organization { Name = name };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org;
    }

    [Fact]
    public async Task Create_WithNewSupabaseUser_ReturnsCreatedUserAndPersistsProfile()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var supabaseUserId = Guid.NewGuid();
        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = email => Task.FromResult(new SupabaseUserResult(supabaseUserId, email))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("new.user@example.com", org.Id, "Admin"));

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var response = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal(supabaseUserId, response.UserId);
        Assert.Equal(org.Id, response.OrgId);
        Assert.Equal("Admin", response.Role);

        var stored = await db.Profiles.FindAsync(supabaseUserId);
        Assert.NotNull(stored);
        Assert.Equal(org.Id, stored!.OrgId);
        Assert.Equal(ProfileRole.Admin, stored.Role);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    public async Task Create_WithInvalidRole_ReturnsBadRequest(string role)
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db, new FakeSupabaseAdminAuthClient());

        var result = await controller.Create(new CreateUserRequest("user@example.com", Guid.NewGuid(), role));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal($"Invalid role '{role}'.", badRequest.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutEmail_ReturnsBadRequest(string email)
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db, new FakeSupabaseAdminAuthClient());

        var result = await controller.Create(new CreateUserRequest(email, Guid.NewGuid(), "Admin"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Email is required.", badRequest.Value);
    }

    [Fact]
    public async Task Create_WithUnknownOrg_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db, new FakeSupabaseAdminAuthClient());
        var unknownOrgId = Guid.NewGuid();

        var result = await controller.Create(new CreateUserRequest("user@example.com", unknownOrgId, "Admin"));

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Organization '{unknownOrgId}' not found.", notFound.Value);
    }

    [Fact]
    public async Task Create_WhenSupabaseUserExistsWithProfileInSameOrg_ReturnsConflict()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var existingUserId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = existingUserId, OrgId = org.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = _ => throw new SupabaseUserAlreadyExistsException("user@example.com"),
            FindUserByEmail = email => Task.FromResult<SupabaseUserResult?>(new SupabaseUserResult(existingUserId, email))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Admin"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("'user@example.com' is already onboarded to this organization.", conflict.Value);
    }

    [Fact]
    public async Task Create_WhenSupabaseUserExistsWithProfileInDifferentOrg_ReturnsConflict()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db, "Org A");
        var otherOrg = await SeedOrganization(db, "Org B");
        var existingUserId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = existingUserId, OrgId = otherOrg.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = _ => throw new SupabaseUserAlreadyExistsException("user@example.com"),
            FindUserByEmail = email => Task.FromResult<SupabaseUserResult?>(new SupabaseUserResult(existingUserId, email))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Admin"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("'user@example.com' is already onboarded to a different organization.", conflict.Value);
    }

    [Fact]
    public async Task Create_WhenSupabaseUserExistsWithoutProfile_ResumesAndCreatesProfile()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var existingUserId = Guid.NewGuid();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = _ => throw new SupabaseUserAlreadyExistsException("user@example.com"),
            FindUserByEmail = email => Task.FromResult<SupabaseUserResult?>(new SupabaseUserResult(existingUserId, email))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Admin"));

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var response = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal(existingUserId, response.UserId);

        var stored = await db.Profiles.FindAsync(existingUserId);
        Assert.NotNull(stored);
        Assert.Equal(org.Id, stored!.OrgId);
    }

    [Fact]
    public async Task Create_WhenSupabaseUserExistsButLookupFails_ReturnsConflict()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);

        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = _ => throw new SupabaseUserAlreadyExistsException("user@example.com"),
            FindUserByEmail = _ => Task.FromResult<SupabaseUserResult?>(null)
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Admin"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(
            "'user@example.com' is already registered in Supabase Auth, but the matching user could not be looked up.",
            conflict.Value);
    }
}
