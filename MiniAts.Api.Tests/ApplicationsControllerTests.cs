using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Controllers;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Tests;

public class ApplicationsControllerTests
{
    private static ApplicationsController CreateController(MiniAtsDbContext db) => new(db, new OrgAccessService());

    private static async Task<(Job Job, Candidate Candidate)> SeedJobAndCandidate(MiniAtsDbContext db, Guid orgId)
    {
        var job = new Job { OrgId = orgId, Title = "Engineer" };
        var candidate = new Candidate { OrgId = orgId, Name = "Jane" };
        db.AddRange(job, candidate);
        await db.SaveChangesAsync();
        return (job, candidate);
    }

    private static async Task<Application> SeedApplication(
        MiniAtsDbContext db, Guid orgId, ApplicationStage stage = ApplicationStage.New, double position = 0)
    {
        var (job, candidate) = await SeedJobAndCandidate(db, orgId);
        var application = new Application
        {
            OrgId = orgId, JobId = job.Id, CandidateId = candidate.Id, Stage = stage, Position = position
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();
        return application;
    }

    [Fact]
    public async Task GetAll_AsCustomer_ReturnsOnlyOwnOrgApplications()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var ownApplication = await SeedApplication(db, own.Id);
        await SeedApplication(db, other.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.GetAll(other.Id, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var application = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ApplicationResponse>>(ok.Value));
        Assert.Equal(ownApplication.Id, application.Id);
    }

    [Fact]
    public async Task GetAll_FilteredByJob_ReturnsOnlyThatJobsApplications()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var first = await SeedApplication(db, own.Id);
        await SeedApplication(db, own.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.GetAll(null, first.JobId, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var application = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ApplicationResponse>>(ok.Value));
        Assert.Equal(first.Id, application.Id);
    }

    [Fact]
    public async Task Create_PlacesNewApplicationAtEndOfNewStage()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var (job, firstCandidate) = await SeedJobAndCandidate(db, own.Id);
        var secondCandidate = new Candidate { OrgId = own.Id, Name = "John" };
        db.Candidates.Add(secondCandidate);
        await db.SaveChangesAsync();
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        await controller.Create(new CreateApplicationRequest(firstCandidate.Id, job.Id), null);
        var result = await controller.Create(new CreateApplicationRequest(secondCandidate.Id, job.Id), null);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApplicationResponse>(created.Value);
        Assert.Equal(nameof(ApplicationStage.New), response.Stage);
        Assert.Equal(1, response.Position);
        Assert.Equal(own.Id, response.OrgId);
    }

    [Fact]
    public async Task Create_WithOtherOrgsCandidate_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var (ownJob, _) = await SeedJobAndCandidate(db, own.Id);
        var (_, otherCandidate) = await SeedJobAndCandidate(db, other.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Create(new CreateApplicationRequest(otherCandidate.Id, ownJob.Id), null);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.False(await db.Applications.AnyAsync());
    }

    [Fact]
    public async Task Update_MovesStageAndPosition_IncludingNegativePositions()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var application = await SeedApplication(db, own.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(application.Id, new UpdateApplicationRequest("interview", -1.5), null);

        Assert.IsType<NoContentResult>(result);
        var stored = await db.Applications.AsNoTracking().SingleAsync(a => a.Id == application.Id);
        Assert.Equal(ApplicationStage.Interview, stored.Stage);
        Assert.Equal(-1.5, stored.Position);
    }

    [Fact]
    public async Task Update_WithInvalidStage_ReturnsBadRequest()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var application = await SeedApplication(db, own.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(application.Id, new UpdateApplicationRequest("Limbo", 1), null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task Update_WithNonFinitePosition_ReturnsBadRequestAndLeavesItUnchanged(double position)
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var application = await SeedApplication(db, own.Id, position: 3);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(application.Id, new UpdateApplicationRequest("Screening", position), null);

        Assert.IsType<BadRequestObjectResult>(result);
        var stored = await db.Applications.AsNoTracking().SingleAsync(a => a.Id == application.Id);
        Assert.Equal(3, stored.Position);
        Assert.Equal(ApplicationStage.New, stored.Stage);
    }

    [Fact]
    public async Task Update_OtherOrgsApplication_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherApplication = await SeedApplication(db, other.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(otherApplication.Id, new UpdateApplicationRequest("Hired", 0), null);

        Assert.IsType<NotFoundResult>(result);
        var stored = await db.Applications.AsNoTracking().SingleAsync(a => a.Id == otherApplication.Id);
        Assert.Equal(ApplicationStage.New, stored.Stage);
    }

    [Fact]
    public async Task Delete_OtherOrgsApplication_ReturnsNotFoundAndKeepsIt()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherApplication = await SeedApplication(db, other.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Delete(otherApplication.Id, null);

        Assert.IsType<NotFoundResult>(result);
        Assert.True(await db.Applications.AnyAsync(a => a.Id == otherApplication.Id));
    }

    [Fact]
    public async Task Delete_OwnApplication_KeepsCandidateAndJob()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var application = await SeedApplication(db, own.Id);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Delete(application.Id, null);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Applications.AnyAsync(a => a.Id == application.Id));
        Assert.True(await db.Candidates.AnyAsync(c => c.Id == application.CandidateId));
        Assert.True(await db.Jobs.AnyAsync(j => j.Id == application.JobId));
    }

    [Fact]
    public async Task AnyEndpoint_CustomerWithoutOrgClaim_ThrowsOrgAccessDenied()
    {
        using var db = TestDb.CreateContext();
        var controller = CreateController(db).As(TestUsers.Principal("Customer", null));

        await Assert.ThrowsAsync<OrgAccessDeniedException>(() => controller.GetAll(null, null, null));
    }
}
