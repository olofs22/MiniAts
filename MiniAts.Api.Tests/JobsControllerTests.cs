using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.Controllers;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Tests;

public class JobsControllerTests
{
    private static JobsController CreateController(MiniAtsDbContext db) => new(db, new OrgAccessService());

    private static async Task<Job> SeedJob(MiniAtsDbContext db, Guid orgId, string title)
    {
        var job = new Job { OrgId = orgId, Title = title };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    [Fact]
    public async Task GetAll_AsCustomer_ReturnsOnlyOwnOrgJobs_EvenWhenRequestingAnotherOrg()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        await SeedJob(db, own.Id, "Own job");
        await SeedJob(db, other.Id, "Other job");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.GetAll(other.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var job = Assert.Single(Assert.IsAssignableFrom<IEnumerable<JobResponse>>(ok.Value));
        Assert.Equal("Own job", job.Title);
    }

    [Fact]
    public async Task GetAll_AsAdminWithOrgId_ReturnsThatOrgsJobs()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        await SeedJob(db, own.Id, "Own job");
        await SeedJob(db, other.Id, "Other job");
        var controller = CreateController(db).As(TestUsers.Admin());

        var result = await controller.GetAll(other.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var job = Assert.Single(Assert.IsAssignableFrom<IEnumerable<JobResponse>>(ok.Value));
        Assert.Equal("Other job", job.Title);
    }

    [Fact]
    public async Task GetById_OtherOrgsJob_ReturnsNotFound()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherJob = await SeedJob(db, other.Id, "Other job");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.GetById(otherJob.Id, null);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_UsesCallersOrg_AndDefaultsToOpen()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Create(new CreateJobRequest("Engineer", null, null), other.Id);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<JobResponse>(created.Value);
        Assert.Equal(own.Id, response.OrgId);
        Assert.Equal(nameof(JobStatus.Open), response.Status);
    }

    [Fact]
    public async Task Create_WithInvalidStatus_ReturnsBadRequest()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Create(new CreateJobRequest("Engineer", null, "Bogus"), null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_OtherOrgsJob_ReturnsNotFoundAndLeavesItUnchanged()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherJob = await SeedJob(db, other.Id, "Other job");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(otherJob.Id, new UpdateJobRequest("Hijacked", null, "Closed"), null);

        Assert.IsType<NotFoundResult>(result);
        var stored = await db.Jobs.AsNoTracking().SingleAsync(j => j.Id == otherJob.Id);
        Assert.Equal("Other job", stored.Title);
    }

    [Fact]
    public async Task Update_OwnJob_PersistsChanges()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var job = await SeedJob(db, own.Id, "Engineer");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Update(job.Id, new UpdateJobRequest("Senior Engineer", "desc", "Closed"), null);

        Assert.IsType<NoContentResult>(result);
        var stored = await db.Jobs.AsNoTracking().SingleAsync(j => j.Id == job.Id);
        Assert.Equal("Senior Engineer", stored.Title);
        Assert.Equal(JobStatus.Closed, stored.Status);
    }

    [Fact]
    public async Task Delete_OtherOrgsJob_ReturnsNotFoundAndKeepsIt()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var otherJob = await SeedJob(db, other.Id, "Other job");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Delete(otherJob.Id, null);

        Assert.IsType<NotFoundResult>(result);
        Assert.True(await db.Jobs.AnyAsync(j => j.Id == otherJob.Id));
    }

    [Fact]
    public async Task Delete_OwnJob_RemovesIt()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var job = await SeedJob(db, own.Id, "Engineer");
        var controller = CreateController(db).As(TestUsers.Customer(own.Id));

        var result = await controller.Delete(job.Id, null);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Jobs.AnyAsync(j => j.Id == job.Id));
    }
}
