using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Controllers;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Tests;

public class CandidatesControllerTests
{
    private static CandidatesController CreateController(MiniAtsDbContext db) => new(db, new OrgAccessService());

    private static async Task<Candidate> SeedCandidate(MiniAtsDbContext db, Guid orgId, string name)
    {
        var candidate = new Candidate { OrgId = orgId, Name = name };
        db.Candidates.Add(candidate);
        await db.SaveChangesAsync();
        return candidate;
    }

    [Fact]
    public async Task GetAll_AsCustomer_ReturnsOnlyOwnOrgCandidates()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        await SeedCandidate(db, own.Id, "Own candidate");
        await SeedCandidate(db, other.Id, "Other candidate");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.GetAll(other.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var candidate = Assert.Single(Assert.IsAssignableFrom<IEnumerable<CandidateResponse>>(ok.Value));
        Assert.Equal("Own candidate", candidate.Name);
    }

    [Fact]
    public async Task GetById_OtherOrgsCandidate_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherCandidate = await SeedCandidate(db, other.Id, "Other candidate");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.GetById(otherCandidate.Id, null);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_UsesCallersOrg()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Create(
            new CreateCandidateRequest("Jane", "jane@example.com", null, null, null), other.Id);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<CandidateResponse>(created.Value);
        Assert.Equal(own.Id, response.OrgId);
        Assert.Equal("jane@example.com", response.Email);
    }

    [Fact]
    public async Task Update_OtherOrgsCandidate_ReturnsNotFoundAndLeavesItUnchanged()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherCandidate = await SeedCandidate(db, other.Id, "Other candidate");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(
            otherCandidate.Id, new UpdateCandidateRequest("Hijacked", null, null, null, null), null);

        Assert.IsType<NotFoundResult>(result);
        var stored = await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == otherCandidate.Id);
        Assert.Equal("Other candidate", stored.Name);
    }

    [Fact]
    public async Task Update_OwnCandidate_PersistsChanges()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var candidate = await SeedCandidate(db, own.Id, "Jane");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(
            candidate.Id, new UpdateCandidateRequest("Jane Doe", null, "123", null, "Strong"), null);

        Assert.IsType<NoContentResult>(result);
        var stored = await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("Jane Doe", stored.Name);
        Assert.Equal("Strong", stored.Notes);
    }

    [Fact]
    public async Task Delete_OtherOrgsCandidate_ReturnsNotFoundAndKeepsIt()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherCandidate = await SeedCandidate(db, other.Id, "Other candidate");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Delete(otherCandidate.Id, null);

        Assert.IsType<NotFoundResult>(result);
        Assert.True(await db.Candidates.AnyAsync(c => c.Id == otherCandidate.Id));
    }

    [Fact]
    public async Task Delete_OwnCandidate_AlsoRemovesTheirApplications()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var candidate = await SeedCandidate(db, own.Id, "Jane");
        var job = new Job { OrgId = own.Id, Title = "Engineer" };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        db.Applications.Add(new Application { OrgId = own.Id, CandidateId = candidate.Id, JobId = job.Id });
        await db.SaveChangesAsync();
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Delete(candidate.Id, null);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Candidates.AnyAsync(c => c.Id == candidate.Id));
        Assert.False(await db.Applications.AnyAsync(a => a.CandidateId == candidate.Id));
    }
}
