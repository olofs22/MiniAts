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
}
