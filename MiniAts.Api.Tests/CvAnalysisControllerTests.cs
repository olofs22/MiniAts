using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MiniAts.Api.Auth;
using MiniAts.Api.Controllers;
using MiniAts.Api.CvAnalysis;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Tests;

public class CvAnalysisControllerTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n fake cv");

    private class FakeCvAnalyzer : ICvAnalyzer
    {
        public IReadOnlyList<CvJobOption>? ReceivedJobs { get; private set; }
        public Func<IReadOnlyList<CvJobOption>, CvAnalysisResult> Respond { get; set; } =
            _ => new CvAnalysisResult(null, null, null, null, []);

        public Task<CvAnalysisResult> AnalyzeAsync(byte[] pdf, IReadOnlyList<CvJobOption> openJobs, CancellationToken ct = default)
        {
            ReceivedJobs = openJobs;
            return Task.FromResult(Respond(openJobs));
        }
    }

    private static CvAnalysisController CreateController(MiniAtsDbContext db, ICvAnalyzer analyzer) =>
        new(db, new OrgAccessService(), analyzer, NullLogger<CvAnalysisController>.Instance);

    private static IFormFile File(byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "cv.pdf");

    private static async Task<Job> SeedJob(MiniAtsDbContext db, Guid orgId, string title, JobStatus status = JobStatus.Open)
    {
        var job = new Job { OrgId = orgId, Title = title, Status = status };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    [Fact]
    public async Task Analyze_NonPdf_ReturnsBadRequestWithoutCallingAnalyzer()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var analyzer = new FakeCvAnalyzer();
        var controller = CreateController(db, analyzer).As(TestUsers.Customer(own.Id));

        var result = await controller.Analyze(File(Encoding.ASCII.GetBytes("PK\u0003\u0004 docx bytes")), null, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(analyzer.ReceivedJobs);
    }

    [Fact]
    public async Task Analyze_MissingFile_ReturnsBadRequest()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var controller = CreateController(db, new FakeCvAnalyzer()).As(TestUsers.Customer(own.Id));

        var result = await controller.Analyze(null, null, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Analyze_SendsOnlyCallersOpenJobs_EvenWhenRequestingAnotherOrg()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var open = await SeedJob(db, own.Id, "Backend developer");
        await SeedJob(db, own.Id, "Old role", JobStatus.Closed);
        await SeedJob(db, own.Id, "Paused role", JobStatus.OnHold);
        await SeedJob(db, other.Id, "Other org role");
        var analyzer = new FakeCvAnalyzer();
        var controller = CreateController(db, analyzer).As(TestUsers.Customer(own.Id));

        await controller.Analyze(File(Pdf), other.Id, default);

        var job = Assert.Single(analyzer.ReceivedJobs!);
        Assert.Equal(open.Id, job.Id);
    }

    [Fact]
    public async Task Analyze_DropsUnknownJobIds_ClampsScores_AndSortsBestFirst()
    {
        using var db = TestDb.CreateContext();
        var (own, other) = await TestUsers.SeedTwoOrgs(db);
        var backend = await SeedJob(db, own.Id, "Backend developer");
        var designer = await SeedJob(db, own.Id, "Designer");
        var otherOrgJob = await SeedJob(db, other.Id, "Other org role");
        var analyzer = new FakeCvAnalyzer
        {
            Respond = _ => new CvAnalysisResult("Jane Doe", "jane@example.com", null, null,
            [
                new CvJobMatch(designer.Id, -5, "No design work"),
                new CvJobMatch(otherOrgJob.Id, 99, "Should never leak"),
                new CvJobMatch(Guid.NewGuid(), 95, "Hallucinated job"),
                new CvJobMatch(backend.Id, 140, "Eight years of C#"),
            ]),
        };
        var controller = CreateController(db, analyzer).As(TestUsers.Customer(own.Id));

        var result = await controller.Analyze(File(Pdf), null, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CvAnalysisResponse>(ok.Value);
        Assert.Equal("Jane Doe", response.Name);
        Assert.Collection(response.Matches,
            m => { Assert.Equal(backend.Id, m.JobId); Assert.Equal("Backend developer", m.JobTitle); Assert.Equal(100, m.Score); },
            m => { Assert.Equal(designer.Id, m.JobId); Assert.Equal(0, m.Score); });
    }

    [Fact]
    public async Task Analyze_WhenNotConfigured_Returns503()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var analyzer = new FakeCvAnalyzer { Respond = _ => throw new CvAnalysisNotConfiguredException() };
        var controller = CreateController(db, analyzer).As(TestUsers.Customer(own.Id));

        var result = await controller.Analyze(File(Pdf), null, default);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
    }

    [Fact]
    public async Task Analyze_WhenModelFails_Returns502WithMessage()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var analyzer = new FakeCvAnalyzer { Respond = _ => throw new CvAnalysisFailedException("The AI model declined to analyse this CV.") };
        var controller = CreateController(db, analyzer).As(TestUsers.Customer(own.Id));

        var result = await controller.Analyze(File(Pdf), null, default);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Equal("The AI model declined to analyse this CV.", Assert.IsType<ProblemDetails>(problem.Value).Detail);
    }
}
