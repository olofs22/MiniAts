using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.CvAnalysis;
using MiniAts.Api.Data;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Controllers;

// Analyses a CV without storing it: the file only lives for the duration of the request.
[ApiController]
[Route("api/candidates/analyze-cv")]
[Authorize]
public class CvAnalysisController(
    MiniAtsDbContext db,
    IOrgAccessService orgAccess,
    ICvAnalyzer analyzer,
    ILogger<CvAnalysisController> logger) : ControllerBase
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxFileBytes + 64 * 1024)]
    public async Task<ActionResult<CvAnalysisResponse>> Analyze(IFormFile? file, [FromQuery] Guid? orgId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A PDF file is required.");
        }

        if (file.Length > MaxFileBytes)
        {
            return BadRequest("The CV must be 10 MB or smaller.");
        }

        byte[] pdf;
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, ct);
            pdf = stream.ToArray();
        }

        // Check the content, not the client-supplied file name or content type.
        if (pdf.Length < 5 || pdf[0] != '%' || pdf[1] != 'P' || pdf[2] != 'D' || pdf[3] != 'F' || pdf[4] != '-')
        {
            return BadRequest("Only PDF files are supported.");
        }

        var effectiveOrgId = orgAccess.ResolveOrgId(User, orgId);

        var openJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrgId == effectiveOrgId && j.Status == JobStatus.Open)
            .Select(j => new CvJobOption(j.Id, j.Title, j.Description))
            .ToListAsync(ct);

        CvAnalysisResult result;
        try
        {
            result = await analyzer.AnalyzeAsync(pdf, openJobs, ct);
        }
        catch (CvAnalysisNotConfiguredException)
        {
            return Problem("CV analysis is not configured on this server.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (CvAnalysisFailedException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "CV analysis call failed.");
            return Problem("Could not analyse this CV right now. Please try again.", statusCode: StatusCodes.Status502BadGateway);
        }

        // Only trust job ids that are actually this org's open jobs, whatever the model returned.
        var jobsById = openJobs.ToDictionary(j => j.Id);
        var matches = result.Matches
            .Where(m => jobsById.ContainsKey(m.JobId))
            .GroupBy(m => m.JobId)
            .Select(g => g.First())
            .Select(m => new CvJobMatchResponse(m.JobId, jobsById[m.JobId].Title, Math.Clamp(m.Score, 0, 100), m.Reason))
            .OrderByDescending(m => m.Score)
            .ToList();

        return Ok(new CvAnalysisResponse(result.Name, result.Email, result.Phone, result.LinkedInUrl, matches));
    }
}
