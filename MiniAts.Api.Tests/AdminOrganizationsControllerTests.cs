using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MiniAts.Api.Controllers;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;
using MiniAts.Api.Supabase;

namespace MiniAts.Api.Tests;

public class AdminOrganizationsControllerTests
{
    private static AdminOrganizationsController CreateController(
        MiniAtsDbContext db, FakeSupabaseAdminAuthClient? supabase = null) =>
        new(db, supabase ?? new FakeSupabaseAdminAuthClient(), NullLogger<AdminOrganizationsController>.Instance);

    [Fact]
    public async Task Create_WithValidName_ReturnsCreatedOrganization()
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db);

        var result = await controller.Create(new CreateOrganizationRequest("Acme Inc"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<OrganizationResponse>(created.Value);
        Assert.Equal("Acme Inc", response.Name);
        Assert.NotEqual(Guid.Empty, response.Id);

        var stored = await db.Organizations.FindAsync(response.Id);
        Assert.NotNull(stored);
        Assert.Equal("Acme Inc", stored!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_WithoutName_ReturnsBadRequest(string? name)
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db);

        var result = await controller.Create(new CreateOrganizationRequest(name!));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Name is required.", badRequest.Value);
    }

    [Fact]
    public async Task Delete_UnknownOrganization_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesOrganizationAndAllItsDataAndBansItsUsers()
    {
        using var db = TestDb.CreateContext();
        var org = new Organization { Name = "Doomed" };
        var otherOrg = new Organization { Name = "Survivor" };
        db.Organizations.AddRange(org, otherOrg);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Profiles.AddRange(
            new Profile { UserId = userId, OrgId = org.Id, Role = ProfileRole.Customer },
            new Profile { UserId = otherUserId, OrgId = otherOrg.Id, Role = ProfileRole.Customer });

        var job = new Job { OrgId = org.Id, Title = "Engineer" };
        var candidate = new Candidate { OrgId = org.Id, Name = "Jane" };
        var otherJob = new Job { OrgId = otherOrg.Id, Title = "Designer" };
        db.AddRange(job, candidate, otherJob);
        await db.SaveChangesAsync();

        db.Applications.Add(new Application
        {
            OrgId = org.Id, JobId = job.Id, CandidateId = candidate.Id, Position = 0
        });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient();
        var controller = CreateController(db, supabase);

        var result = await controller.Delete(org.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Organizations.AnyAsync(o => o.Id == org.Id));
        Assert.False(await db.Profiles.AnyAsync(p => p.OrgId == org.Id));
        Assert.False(await db.Jobs.AnyAsync(j => j.OrgId == org.Id));
        Assert.False(await db.Candidates.AnyAsync(c => c.OrgId == org.Id));
        Assert.False(await db.Applications.AnyAsync(a => a.OrgId == org.Id));
        Assert.Equal([userId], supabase.BannedUserIds);

        Assert.True(await db.Organizations.AnyAsync(o => o.Id == otherOrg.Id));
        Assert.True(await db.Profiles.AnyAsync(p => p.UserId == otherUserId));
        Assert.True(await db.Jobs.AnyAsync(j => j.Id == otherJob.Id));
    }

    [Fact]
    public async Task Delete_WhenBanFails_StillDeletesOrganization()
    {
        using var db = TestDb.CreateContext();
        var org = new Organization { Name = "Doomed" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        db.Profiles.Add(new Profile { UserId = Guid.NewGuid(), OrgId = org.Id, Role = ProfileRole.Customer });
        await db.SaveChangesAsync();

        var supabase = new FakeSupabaseAdminAuthClient
        {
            BanUser = _ => throw new SupabaseAdminApiException(System.Net.HttpStatusCode.BadGateway, "down")
        };
        var controller = CreateController(db, supabase);

        var result = await controller.Delete(org.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Organizations.AnyAsync(o => o.Id == org.Id));
    }
}
