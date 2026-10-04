using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        new(db, supabase, new ConfigurationBuilder().Build(), NullLogger<AdminUsersController>.Instance);

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

        var result = await controller.Create(new CreateUserRequest("new.user@example.com", org.Id, "Customer"));

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var response = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal(supabaseUserId, response.UserId);
        Assert.Equal(org.Id, response.OrgId);
        Assert.Equal("Customer", response.Role);

        var stored = await db.Profiles.FindAsync(supabaseUserId);
        Assert.NotNull(stored);
        Assert.Equal(org.Id, stored!.OrgId);
        Assert.Equal(ProfileRole.Customer, stored.Role);
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

        var result = await controller.Create(new CreateUserRequest(email, Guid.NewGuid(), "Customer"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Email is required.", badRequest.Value);
    }

    [Fact]
    public async Task Create_WithUnknownOrg_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db, new FakeSupabaseAdminAuthClient());
        var unknownOrgId = Guid.NewGuid();

        var result = await controller.Create(new CreateUserRequest("user@example.com", unknownOrgId, "Customer"));

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Organization '{unknownOrgId}' not found.", notFound.Value);
    }

    [Fact]
    public async Task Create_AdminWithOrgId_ReturnsBadRequest()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var controller = CreateController(db, new FakeSupabaseAdminAuthClient());

        var result = await controller.Create(new CreateUserRequest("admin@example.com", org.Id, "Admin"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Admin users cannot belong to an organization.", badRequest.Value);
        Assert.Empty(db.Profiles);
    }

    [Fact]
    public async Task Create_AdminWithoutOrgId_CreatesProfileWithNullOrg()
    {
        using var db = TestDb.CreateContext();
        var supabaseUserId = Guid.NewGuid();
        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = email => Task.FromResult(new SupabaseUserResult(supabaseUserId, email))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("admin@example.com", null, "Admin"));

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var stored = await db.Profiles.FindAsync(supabaseUserId);
        Assert.NotNull(stored);
        Assert.Null(stored!.OrgId);
        Assert.Equal(ProfileRole.Admin, stored.Role);
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

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Customer"));

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

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Customer"));

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

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Customer"));

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

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Customer"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(
            "'user@example.com' is already registered in Supabase Auth, but the matching user could not be looked up.",
            conflict.Value);
    }

    [Fact]
    public async Task Create_WhenSupabaseRejectsInvite_ReturnsUnprocessableEntity()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);

        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = _ => throw new SupabaseAdminApiException(HttpStatusCode.InternalServerError, "boom")
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Customer"));

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        Assert.Contains("user@example.com", (string)unprocessable.Value!);
    }

    // Note: AdminUsersController.CreateProfile also has a branch that catches DbUpdateException
    // specifically wrapping a Postgres unique-violation (a genuine insert race between two
    // concurrent requests) and returns 409 instead of 500. That branch pattern-matches on
    // Npgsql's PostgresException, which Sqlite never throws (it raises its own exception type
    // for the same underlying constraint violation), so it can't be exercised against this
    // in-memory Sqlite database. Covering it would require a real Postgres connection.
    [Fact]
    public async Task Create_WhenProfileInsertFails_ReturnsPartialFailureResponse()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var collidingUserId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = collidingUserId, OrgId = org.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();
        // Detach the seeded row so the controller's insert below hits the database's own
        // unique-constraint check (a real DbUpdateException) instead of EF's client-side
        // identity-map conflict, which is a different exception the controller doesn't catch.
        db.ChangeTracker.Clear();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            InviteUserByEmail = email => Task.FromResult(new SupabaseUserResult(collidingUserId, email))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Create(new CreateUserRequest("user@example.com", org.Id, "Customer"));

        var serverError = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var response = Assert.IsType<CreateUserPartialFailureResponse>(serverError.Value);
        Assert.Equal(collidingUserId, response.SupabaseUserId);
        Assert.Equal(org.Id, response.OrgId);
    }

    [Fact]
    public async Task GetAll_WithOrgId_ReturnsOnlyThatOrgsUsersWithEmails()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var otherOrg = await SeedOrganization(db, "Other Inc");
        var userId = Guid.NewGuid();
        db.Profiles.AddRange(
            new Profile { UserId = userId, OrgId = org.Id, Role = ProfileRole.Customer },
            new Profile { UserId = Guid.NewGuid(), OrgId = otherOrg.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            GetUserById = id => Task.FromResult<SupabaseUserResult?>(new SupabaseUserResult(id, "jane@example.com"))
        };
        var controller = CreateController(db, supabase);

        var result = await controller.GetAll(org.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<UserResponse>>(ok.Value).ToList();
        var user = Assert.Single(users);
        Assert.Equal(userId, user.UserId);
        Assert.Equal("jane@example.com", user.Email);
    }

    [Fact]
    public async Task GetAll_WhenSupabaseUserMissing_ReturnsUnknownEmail()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        db.Profiles.Add(new Profile { UserId = Guid.NewGuid(), OrgId = org.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            GetUserById = _ => Task.FromResult<SupabaseUserResult?>(null)
        };
        var controller = CreateController(db, supabase);

        var result = await controller.GetAll(org.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.Single(Assert.IsAssignableFrom<IEnumerable<UserResponse>>(ok.Value));
        Assert.Equal("(unknown)", user.Email);
    }

    [Fact]
    public async Task Delete_BansSupabaseUserAndRemovesProfile()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var userId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = userId, OrgId = org.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient();
        var controller = CreateController(db, supabase);

        var result = await controller.Delete(userId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal([userId], supabase.BannedUserIds);
        Assert.Null(await db.Profiles.FindAsync(userId));
    }

    [Fact]
    public async Task Delete_UnknownUser_ReturnsNotFoundWithoutBanning()
    {
        using var db = TestDb.CreateContext();
        var supabase = new FakeSupabaseAdminAuthClient();
        var controller = CreateController(db, supabase);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(supabase.BannedUserIds);
    }

    [Fact]
    public async Task Delete_WhenBanFails_KeepsProfile()
    {
        using var db = TestDb.CreateContext();
        var org = await SeedOrganization(db);
        var userId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = userId, OrgId = org.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            BanUser = _ => throw new SupabaseAdminApiException(HttpStatusCode.BadGateway, "down")
        };
        var controller = CreateController(db, supabase);

        await Assert.ThrowsAsync<SupabaseAdminApiException>(() => controller.Delete(userId));
        Assert.NotNull(await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId));
    }
}
